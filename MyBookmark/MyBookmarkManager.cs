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

    class BookmarkPrim
    {
        int m_line;
        string m_comment;       // ブックマークのコメント
        string m_that;          // その行の文字列
        string m_next;          // 次の行の文字列
    }

    class MyBookmarkManager
    {
        public ConcurrentDictionary<string, BookmarkPrim> m_BookmarkPrims { get; set; }

        private void Execute(object sender, EventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("F1Help has been invoked.");
        }

        public void AddBookmark()
        {

        }



        //=================================================================================================
        // static
        static private MyBookmarkManager s_Instandce;
        static private string s_bookmarkFileName;
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


        public static void SetView(IWpfTextView view, SVsServiceProvider serviceProvider)
        {
            view.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document);

            DTE dte = (DTE)serviceProvider.GetService(typeof(DTE));
            ProjectItem projectItem = dte.Solution.FindProjectItem(document.FilePath);

            if (projectItem != null && projectItem.ContainingProject != null)
            {
                string solutionDirectory = "";
                string projectPath = projectItem.ContainingProject.FileName;
                if (projectPath != "") // projectPath will be empty if file isn't part of a projec+t.
                {
                    s_projectDirectory = Path.GetDirectoryName(projectPath) + @"\";
                }

                string solutionPath = dte.Solution.FileName;
                if (solutionPath != "") // solutionPath will be empty if project isn't part of a saved solution
                {
                    solutionDirectory = Path.GetDirectoryName(solutionPath) + @"\";
                }

                if (s_solutionDirectory != solutionDirectory)
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
        }
    }
}

