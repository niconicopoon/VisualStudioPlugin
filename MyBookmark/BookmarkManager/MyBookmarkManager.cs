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
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using Codeplex.Data;

// using System.Windows.Controls;
// using System.Runtime.Serialization.Json;
// using System.Runtime.Serialization;
// using static System.Console;
// using System.Text.Json;
// using System.Text.Json.Serialization;

namespace MyBookmark
{
    [Serializable]
    public class BookmarkPrim
    {
        public string m_comment { get; set; }       // ブックマークのコメント
        public string m_that { get; set; }          // その行の文字列
        public string m_next { get; set; }          // 次の行の文字列

        public string m_tag;
        public string m_line;

        public void SetTag()
        {
            System.IO.StringReader rs = new System.IO.StringReader(m_comment);
            m_line = rs.ReadLine();
            int index = m_line.IndexOf(' ');
            if (index < 0)
            {
                m_tag = m_line;
            }
            else
            {
                m_tag = m_line.Substring(0, index);
            }

            m_that = Util.GetWithoutUselessCharacter(m_that);
            m_next = Util.GetWithoutUselessCharacter(m_next);
        }

        /* public void Serialize(BinaryWriter writer)
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
        } */
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
    }

    class FileBookmarkPrims : ConcurrentDictionary<string, BookmarkPrims>
    {
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

        static public void Jump(string str)
        {
            int idx = str.IndexOf('[');
            if (idx >= 0)
            {
                idx++;
                string fileName = str.Substring(idx);
                int lineNo = int.Parse(fileName.Substring(fileName.IndexOf(':') + 1));
                fileName = fileName.Substring(0, fileName.IndexOf(']'));

                for (int docIndex = 1; docIndex <= s_dte.Documents.Count; docIndex++)
                {
                    Document document = s_dte.Documents.Item(docIndex);
                    string docFileName = RelativeFileName(document.FullName);
                    if (docFileName == fileName)
                    {
                        document.Activate();
                        EnvDTE.TextSelection textSelection = (EnvDTE.TextSelection)(document.Selection);
                        textSelection.GotoLine(lineNo, true);
                        // for(int winIndex=1; winIndex<=document.Windows.Count; winIndex++)
                        if (document.Windows.Count > 0)
                        {
                            document.Windows.Item(1).Activate();
                        }
                        // for(int winIndex=1; winIndex<= s_dte.Windows.Count; winIndex++)
                        break;
                    }
                }
            }
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

        public BookmarkPrims CreateBookmarkPrims(CommentsManager commentsManager)
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
            return bookmarkPrims;
        }

        public BookmarkPrims GetBookmarkPrims()
        {
            return m_FileBookmarkPrims[s_fileName];
        }

        public struct VBookmarkPrim
        {
            public VBookmarkPrim(string fileName, int lineNo, BookmarkPrim bookmarkPrim)
            {
                m_FileName = fileName;
                m_LineNo = lineNo;
                m_BookmarkPrim = bookmarkPrim;
            }
            public string m_FileName;
            public int m_LineNo;
            public BookmarkPrim m_BookmarkPrim;
        };
        public void RedrawToolWindow()
        {
            ConcurrentDictionary<string, List<VBookmarkPrim>> labels = new ConcurrentDictionary<string, List<VBookmarkPrim>>();

            foreach(var bookmarkPrims in m_FileBookmarkPrims)
            {
                foreach (var bookmarkPrimIt in bookmarkPrims.Value)
                {
                    BookmarkPrim bookmarkPrim = bookmarkPrimIt.Value;
                    List<VBookmarkPrim> bookmarkPrimList = null;
                    if (labels.ContainsKey(bookmarkPrim.m_tag))
                    {
                        bookmarkPrimList = labels[bookmarkPrim.m_tag];
                    }
                    else
                    {
                        bookmarkPrimList = new List<VBookmarkPrim>();
                        labels.TryAdd(bookmarkPrim.m_tag, bookmarkPrimList);
                    }
                    VBookmarkPrim vBookmarkPrim = new VBookmarkPrim(bookmarkPrims.Key, bookmarkPrimIt.Key, bookmarkPrim);
                    bookmarkPrimList.Add(vBookmarkPrim);
                }
            }

            /* ToolWindowControl toolWindowControl = ToolWindowControl.GetInstance();
            System.Windows.Forms.TreeView treeView = toolWindowControl.m_BookmarkTreeView; */
            var treeView = ToolWindowControl.GetBookmarkTreeView();

            treeView.Items.Clear();
            foreach (var label in labels)
            {
                TreeViewItem labelItem = new TreeViewItem();
                labelItem.Header = label.Key.ToString();
                treeView.Items.Add(labelItem);
                foreach (var vBookmarkPrim in label.Value)
                {
                    TreeViewItem item = new TreeViewItem();
                    item.Header = vBookmarkPrim.m_BookmarkPrim.m_line + " [" + vBookmarkPrim.m_FileName + "]:" + vBookmarkPrim.m_LineNo;
                    labelItem.Items.Add(item);
                }
            }
        }

        private void EditBookmark(BookmarkPrim prim)
        {
            CommentEditor commentEditor = new CommentEditor();
            commentEditor.editTextBox.Text = prim.m_comment;
            commentEditor.ShowDialog();
            prim.m_comment = commentEditor.editTextBox.Text;
            prim.SetTag();
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
                bookmarkPrims.TryAdd(lineNo, prim);
                bookmarkPrims.GetCommentsManager().SetBookmark(bookmarkPrims);
                Save();
                RedrawToolWindow();
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
            RedrawToolWindow();
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

        public static bool Save()
        {
            try
            {
                dynamic jsonSerialize = DynamicJson.Serialize(s_Instandce.m_FileBookmarkPrims);
                dynamic json = DynamicJson.Parse(jsonSerialize);
                if (json != null)
                {
                    using (StreamWriter s = new StreamWriter(s_bookmarkFileName))
                    {
                        s.Write(json.ToString());
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static string RelativeFileName(string fileName)
        {
            int idx = fileName.IndexOf(s_solutionDirectory);
            if (idx == 0)
            {
                fileName = fileName.Substring(s_solutionDirectory.Length);
            }
            return fileName;
        }

        public static void SetFileName(string fileName)
        {
            s_fileName = RelativeFileName(fileName);
        }

        public static bool Load(CommentsManager commentsManager)
        {
            try
            {
                s_Instandce = new MyBookmarkManager();

                using (StreamReader s = new StreamReader(s_bookmarkFileName))
                {
                    string data = s.ReadToEnd();
                    var json = DynamicJson.Parse(data);
                    if (json != null)
                    {
                        foreach (string array1index in json.GetDynamicMemberNames())
                        {
                            var bpfjson = json[int.Parse(array1index)];
                            foreach (string bpf in bpfjson.GetDynamicMemberNames())
                            {
                                if(bpf == "Key")
                                {
                                    SetFileName(bpfjson[bpf]);
                                }
                                else if (bpf == "Value")
                                {
                                    foreach (string array2index in bpfjson[bpf].GetDynamicMemberNames())
                                    {
                                        var bpjson = bpfjson[bpf][int.Parse(array2index)];

                                        BookmarkPrims bookmarkPrims = s_Instandce.CreateBookmarkPrims(commentsManager);
                                        dynamic lineNo = null;
                                        foreach (string bp in bpjson.GetDynamicMemberNames())
                                        {
                                            if (bp == "Key")
                                            {
                                                lineNo = bpjson[bp];
                                            }
                                            else if (bp == "Value")
                                            {
                                                BookmarkPrim prim = bpjson[bp].Deserialize<BookmarkPrim>();
                                                prim.SetTag();
                                                bookmarkPrims.TryAdd(int.Parse(lineNo.ToString()), prim);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    s_Instandce.RedrawToolWindow();
                }
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
                    SetFileName(commentsManager.m_FileName);
                }
                else
                {       // 新しい Solution が選択された
                    if (s_Instandce != null && s_bookmarkFileName != "")
                    {
                        Save();
                    }

                    s_solutionDirectory = solutionDirectory;
                    SetFileName(commentsManager.m_FileName);

                    // s_bookmarkDirectory = @"C:\MyProj\temp\mbook\";
                    s_bookmarkDirectory = s_solutionDirectory + @"\MyBookmark\";
                    Directory.CreateDirectory(s_bookmarkDirectory);

                    s_bookmarkFileName = s_solutionDirectory.Replace(':', '_');
                    s_bookmarkFileName = s_bookmarkFileName.Replace('\\', '-');
                    s_bookmarkFileName = s_bookmarkDirectory + s_bookmarkFileName + @".mbk";

                    Load(commentsManager);
                }
            }

            SetFileName(commentsManager.m_FileName);
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

