using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace MyBookmark
{
    public class BookmarkPrim
    {
        public string m_comment;       // ブックマークのコメント
        public string m_that;          // その行の文字列
        public string m_next;          // 次の行の文字列

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(m_comment);
            writer.Write(m_that);
            writer.Write(m_next);
        }

        public void Deserialize(BinaryReader reader)
        {
            m_comment = reader.ReadString();
            m_that = reader.ReadString();
            m_next = reader.ReadString();
        }
    }

    class BookmarkPrims : ConcurrentDictionary<int, BookmarkPrim>
    {
        private CommentsManager m_CommentsManager;

        public CommentsManager GetCommentsManager()
        {
            return m_CommentsManager;
        }

        public void SetCommentsManager(CommentsManager commentsManager)
        {
            m_CommentsManager = commentsManager;
        }

        public BookmarkPrims(CommentsManager commentsManager)
        {
            SetCommentsManager(commentsManager);
        }


        public void Serialize(BinaryWriter writer)
        {
            writer.Write((int)this.Count);
            foreach (var it in this)
            {
                writer.Write(it.Key);
                it.Value.Serialize(writer);
            }
        }
        public void Deserialize(BinaryReader reader)
        {
            int ct = reader.ReadInt32();
            for (int i = 0; i < ct; i++)
            {
                int key = reader.ReadInt32();
                BookmarkPrim bookmarkPrim = new BookmarkPrim();
                bookmarkPrim.Deserialize(reader);
                this.TryAdd(key, bookmarkPrim);
            }
        }
    }

    class FileBookmarkPrims : ConcurrentDictionary<string, BookmarkPrims>
    {
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(this.Count);
            foreach(var it in this)
            {
                writer.Write(it.Key);
                it.Value.Serialize(writer);
            }
        }
        public void Deserialize(BinaryReader reader)
        {
            int ct = reader.ReadInt32();
            for (int i = 0; i < ct; i++)
            {
                string key = reader.ReadString();
                BookmarkPrims bookmarkPrims = new BookmarkPrims(null);
                bookmarkPrims.Deserialize(reader);
                this.TryAdd(key, bookmarkPrims);
            }
        }
    }

    class MyBookmarkManager
    {
        // public static IWpfTextView s_CurView;
        public static DTE s_dte;

        public FileBookmarkPrims m_FileBookmarkPrims { get; set; }

        MyBookmarkManager()
        {
            m_FileBookmarkPrims = new FileBookmarkPrims();
        }

        private void Execute(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("F1Help has been invoked.");
        }

        static EnvDTE.TextSelection GetTextSelection()
        {
            /* EnvDTE80.DTE2 dte2 = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE");
                dte2.MainWindow.Activate();
                dte2.Events.WindowEvents.WindowActivated += OnWindowActivated;
                return (EnvDTE.TextSelection)dte2.ActiveDocument.Selection; */

            // s_dte.MainWindow.Activate();
            // return (EnvDTE.TextDocument)s_dte.ActiveDocument.Object("TextDocument");

            return (EnvDTE.TextSelection)s_dte.ActiveDocument.Selection;
        }

        static public int GetCursorLineNo()
        {
            // EnvDTE.TextPoint point = GetTextDocument().StartPoint;
            // EnvDTE.TextPoint objCursorTextPoint = GetTextDocument().Selection.ActivePoint;
            EnvDTE.TextPoint objCursorTextPoint = GetTextSelection().ActivePoint;
            return objCursorTextPoint.Line;
        }

        /* private void OnWindowActivated(EnvDTE.Window GotFocus, EnvDTE.Window LostFocus)
        {
        } */

        public void CreateBookmarkPrims(CommentsManager commentsManager)
        {
            BookmarkPrims bookmarkPrims = null;
            if (!m_FileBookmarkPrims.ContainsKey(s_fileName))
            {
                bookmarkPrims = new BookmarkPrims(commentsManager);
                m_FileBookmarkPrims.TryAdd(s_fileName, bookmarkPrims);
            } else
            {
                bookmarkPrims = m_FileBookmarkPrims[s_fileName];
                bookmarkPrims.SetCommentsManager(commentsManager);
            }
            commentsManager.SetBookmark(bookmarkPrims);
        }

        public BookmarkPrims GetBookmarkPrims()
        {
            return m_FileBookmarkPrims[s_fileName];
        }
        
        private void EditBookmark(BookmarkPrim prim)
        {
            CommentEditor commentEditor = new CommentEditor();
            commentEditor.editTextBox.Text = prim.m_comment;
            commentEditor.ShowDialog();
            prim.m_comment = commentEditor.editTextBox.Text;
            commentEditor.Dispose();
        }

        // ブックマークを追加、すでにあるなら edit する
        public void AddEditBookmark()
        {
            // if (s_CurView == null) return;

            // EnvDTE.TextDocument textDocument = GetTextDocument();
            EnvDTE.TextSelection textSelection = GetTextSelection();
            if (textSelection != null)
            {
                Int32 lineNo = GetCursorLineNo();
                BookmarkPrims bookmarkPrims = GetBookmarkPrims();
                BookmarkPrim prim = null;
                if (bookmarkPrims.ContainsKey(lineNo))
                {       // BookmarkPrim を edit する
                    prim = bookmarkPrims[lineNo];
                } else
                {       // BookmarkPrim 作る
                    prim = new BookmarkPrim();
                    prim.m_comment = "test";
                    textSelection.SelectLine();
                    prim.m_that = textSelection.Text;
                    textSelection.GotoLine(lineNo + 1);
                    textSelection.Cancel();
                    textSelection.SelectLine();
                    prim.m_next = textSelection.Text;
                    bookmarkPrims.TryAdd(lineNo, prim);
                }
                EditBookmark(prim);
                bookmarkPrims[lineNo] = prim;
                bookmarkPrims.GetCommentsManager().SetBookmark(bookmarkPrims);
                Save();
            }
        }

        public void DelBookmark()
        {
            // if (s_CurView == null) return;

            BookmarkPrim prim = null;
            BookmarkPrims bookmarkPrims = GetBookmarkPrims();
            int lineNo = GetCursorLineNo();
            bookmarkPrims.TryRemove(lineNo, out prim);
            bookmarkPrims.GetCommentsManager().DelBookmark(lineNo);
            Save();
        }


        //=================================================================================================
        // static
        static private MyBookmarkManager s_Instandce;
        static private string s_bookmarkDirectory;
        static private string s_bookmarkFileName;
        static private string s_fileName;
        static private string s_projectDirectory;
        static private string s_solutionDirectory;

        public static MyBookmarkManager GetInstance()
        {
            return s_Instandce;
        }

        /*
        public static bool BinarySerialize<T>(string filePath, T dataObject)
        {
            try
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Create);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(fileStream, dataObject);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool BinaryDeserialize<T>(string filePath, out T dataObject)
        {
            try
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Open);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                var obj = binaryFormatter.Deserialize(fileStream);
                dataObject = (T)obj;
            }
            catch
            {
                dataObject = default(T);
                return false;
            }
            return true;
        }
        */

        public static bool Save()
        {
            try
            {
                FileStream fileStream = new FileStream(s_bookmarkFileName, FileMode.Create);
                BinaryWriter writer = new BinaryWriter(fileStream);
                s_Instandce.m_FileBookmarkPrims.Serialize(writer);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool Load()
        {
            try
            {
                s_Instandce = new MyBookmarkManager();
                s_Instandce.m_FileBookmarkPrims = new FileBookmarkPrims();

                FileStream fileStream = new FileStream(s_bookmarkFileName, FileMode.Open);
                BinaryReader reader = new BinaryReader(fileStream);
                s_Instandce.m_FileBookmarkPrims.Deserialize(reader);
            }
            catch
            {
                return false;
            }
            return true;

            // return BinaryDeserialize<MyBookmarkManager>(s_bookmarkFileName, out s_Instandce);      // ブックマーク読み込み
        }

        public static void CloseView(CommentsManager commentsManager)
        {
            if(commentsManager.m_FileName == s_fileName)
            {
                s_fileName = null;
            }
        }

        public static void SetView(CommentsManager commentsManager, SVsServiceProvider serviceProvider)
        {
            s_dte = (DTE)serviceProvider.GetService(typeof(DTE));

            // s_CurView = commentsManager.GetView();

            commentsManager.GetView().TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document);
            ProjectItem projectItem = s_dte.Solution.FindProjectItem(document.FilePath);

            string backFileName = s_fileName;
            s_fileName = commentsManager.m_FileName;

            if (projectItem != null && projectItem.ContainingProject != null)
            {
                string solutionDirectory = "";
                string projectPath = projectItem.ContainingProject.FileName;
                if (projectPath != "") // projectPath will be empty if file isn't part of a projec+t.
                {
                    s_projectDirectory = Path.GetDirectoryName(projectPath) + @"\";
                }

                string solutionPath = s_dte.Solution.FileName;
                if (solutionPath != "") // solutionPath will be empty if project isn't part of a saved solution
                {
                    solutionDirectory = Path.GetDirectoryName(solutionPath) + @"\";
                }

                if (s_solutionDirectory == solutionDirectory)
                {       // Solution は同じ
                    

                } else
                {       // 新しい Solution が選択された
                    if (s_Instandce != null && s_bookmarkFileName != "")
                    {
                        Save();
                    }

                    s_solutionDirectory = solutionDirectory;

                    s_bookmarkDirectory = @"C:\MyProj\temp\mbook\";
                    Directory.CreateDirectory(s_bookmarkDirectory);

                    s_bookmarkFileName = s_solutionDirectory.Replace(':', '_');
                    s_bookmarkFileName = s_bookmarkFileName.Replace('\\', '-');
                    s_bookmarkFileName = s_bookmarkDirectory + s_bookmarkFileName + @".mbk";

                    Load();
                }
            }

            if (s_fileName != backFileName)
            {
                if (s_Instandce != null)
                {
                    s_Instandce.CreateBookmarkPrims(commentsManager);           // m_FileBookmarkPrims.TryAdd(s_fileName, bookmarkPrims); する
                }
            }
        }
    }
}

