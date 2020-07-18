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

namespace MyBookmark
{
    public class BookmarkPrim
    {
        public string m_comment;       // ブックマークのコメント
        public string m_that;          // その行の文字列
        public string m_next;          // 次の行の文字列
    }

    class BookmarkPrims : ConcurrentDictionary<int, BookmarkPrim>
    {
       private CommentsManager m_CommentsManager;

        public CommentsManager GetCommentsManager()
        {
            return m_CommentsManager;
        }

        public BookmarkPrims(CommentsManager commentsManager)
        {
            m_CommentsManager = commentsManager;
        }
    }
    class FileBookmarkPrims : ConcurrentDictionary<string, BookmarkPrims> { }


    class MyBookmarkManager
    {
        public static IWpfTextView s_CurView;
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
            if (!m_FileBookmarkPrims.ContainsKey(s_fileName))
            {
                BookmarkPrims bookmarkPrims = new BookmarkPrims(commentsManager);
                m_FileBookmarkPrims.TryAdd(s_fileName, bookmarkPrims);
            }
        }

        public BookmarkPrims GetBookmarkPrims()
        {
            return m_FileBookmarkPrims[s_fileName];
        }

        public void AddBookmark()
        {
            // if (s_CurView == null) return;

            // EnvDTE.TextDocument textDocument = GetTextDocument();
            EnvDTE.TextSelection textSelection = GetTextSelection();
            if (textSelection != null)
            {
                BookmarkPrim prim = new BookmarkPrim();
                Int32 lineNo = GetCursorLineNo();
                prim.m_comment = "test";
                textSelection.SelectLine();
                prim.m_that = textSelection.Text;
                textSelection.GotoLine(lineNo+1);
                textSelection.Cancel();
                textSelection.SelectLine();
                prim.m_next = textSelection.Text;
                BookmarkPrims bookmarkPrims = GetBookmarkPrims();
                bookmarkPrims.TryAdd(lineNo, prim);
                bookmarkPrims.GetCommentsManager().SetBookmark(bookmarkPrims);
            }
        }

        public void DelBookmark()
        {
            // if (s_CurView == null) return;

            BookmarkPrim prim = null;
            BookmarkPrims bookmarkPrims = GetBookmarkPrims();
            bookmarkPrims.TryRemove(GetCursorLineNo(), out prim);
            bookmarkPrims.GetCommentsManager().SetBookmark(bookmarkPrims);
        }


        //=================================================================================================
        // static
        static private MyBookmarkManager s_Instandce;
        static private string s_bookmarkFileName;
        static private string s_fileName;
        static private string s_projectDirectory;
        static private string s_solutionDirectory;

        public static MyBookmarkManager GetInstance()
        {
            return s_Instandce;
        }
        public static bool BinarySerialize<T>(string filePath, T dataObject)
        {
            try
            {

                FileStream fileStream =
                    new FileStream(
                        filePath,
                        FileMode.Create);

                BinaryFormatter binaryFormatter =
                    new BinaryFormatter();

                binaryFormatter.Serialize(
                    fileStream,
                    dataObject);

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

                FileStream fileStream =
                    new FileStream(
                        filePath,
                        FileMode.Open);

                BinaryFormatter binaryFormatter =
                    new BinaryFormatter();

                var obj =
                    binaryFormatter.Deserialize(
                        fileStream);

                dataObject = (T)obj;

            }
            catch
            {
                dataObject = default(T);
                return false;
            }
            return true;
        }


        public static void SetView(CommentsManager commentsManager, SVsServiceProvider serviceProvider)
        {
            s_dte = (DTE)serviceProvider.GetService(typeof(DTE));

            s_CurView = commentsManager.GetView();
            s_CurView.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document);

            ProjectItem projectItem = s_dte.Solution.FindProjectItem(document.FilePath);
            string backFileName = s_fileName;
            s_fileName = document.FilePath;

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
                        BinarySerialize<MyBookmarkManager>(s_bookmarkFileName, s_Instandce);        // ブックマーク書き込み
                    }

                    s_solutionDirectory = solutionDirectory;
                    s_bookmarkFileName = s_solutionDirectory.Replace(':', '_');
                    s_bookmarkFileName = s_bookmarkFileName.Replace('\\', '-');
                    s_bookmarkFileName = @"F:\MyProj\temp\mbook\" + s_bookmarkFileName + @".mbk";
                    // Directory.CreateDirectory(s_bookmarkDirectory);

                    BinaryDeserialize<MyBookmarkManager>(s_bookmarkFileName, out s_Instandce);      // ブックマーク読み込み
                    if (s_Instandce == null)
                    {
                        s_Instandce = new MyBookmarkManager();
                    }
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

