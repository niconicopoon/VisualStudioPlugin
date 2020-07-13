﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MyBookmark
{
   /// <summary>
   /// This class provides variable substitution for strings, e.g. replacing '$(ProjectDir)' with 'C:\MyProject\'
   /// Currently supported variables are:
   ///   $(ProjectDir)
   ///   $(SolutionDir)
   /// </summary>
   public class VariableExpander
   {
      private readonly Regex _variableMatcher;
      private const string VARIABLE_PATTERN = @"\$\(\S+?\)";

      private const string PROJECTDIR_PATTERN = "$(ProjectDir)";
      private const string SOLUTIONDIR_PATTERN = "$(SolutionDir)";

      private string _projectDirectory;
      private string _solutionDirectory;

      private readonly IWpfTextView _view;
      private SVsServiceProvider _serviceProvider = null;

      public VariableExpander(IWpfTextView view, SVsServiceProvider serviceProvider)
      {
         _view = view ?? throw new ArgumentNullException(nameof(view));
         _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

         _variableMatcher = new Regex(VARIABLE_PATTERN, RegexOptions.Compiled);
         try
         {
            populateVariableValues();
         }
         catch (Exception ex)
         {
            ExceptionHandler.Notify(ex, true);
         }
      }

      /// <summary>
      /// Processes URL by replacing $(Variables) with their values
      /// </summary>
      /// <param name="urlString">Input URL string</param>
      /// <returns>Processed URL string</returns>
      public string ProcessText(string urlString)
      {
         string processedText = _variableMatcher.Replace(urlString, evaluator);
         return processedText;
      }

      /// <summary>
      /// Regex.Replace Match evaluator callback. Performs variable name/value substitution
      /// </summary>
      /// <param name="match"></param>
      /// <returns></returns>
      private string evaluator(Match match)
      {
         string variableName = match.Value;
         if (string.Compare(variableName, PROJECTDIR_PATTERN, StringComparison.InvariantCultureIgnoreCase) == 0)
         {
            return _projectDirectory;
         }
         else if (string.Compare(variableName, SOLUTIONDIR_PATTERN, StringComparison.InvariantCultureIgnoreCase) == 0)
         {
            return _solutionDirectory;
         }
         else
         {
            // Could throw an exception here, but it's possible the path contains $(...).
            // TODO: Variable name escaping
            return variableName;
         }
      }

      /// <summary>
      /// Populates variable values from the ProjectItem associated with the TextView.
      /// </summary>
      /// <remarks>Based on code from http://stackoverflow.com/a/2493865
      /// Guarantees variables will not be null, but they may be empty if e.g. file isn't part of a project, or solution hasn't been saved yet
      /// TODO: If additional variables are added that reference the path to the document, handle cases of 'Save as' / renaming
      /// </remarks>
      private void populateVariableValues()
      {
         _projectDirectory = "";
         _solutionDirectory = "";

         _view.TextDataModel.DocumentBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document);

         DTE dte = (DTE)_serviceProvider.GetService(typeof(DTE));
         ProjectItem projectItem = dte.Solution.FindProjectItem(document.FilePath);

         if (projectItem != null && projectItem.ContainingProject != null)
         {
            string projectPath = projectItem.ContainingProject.FileName;
            if (projectPath != "") // projectPath will be empty if file isn't part of a project.
            {
               _projectDirectory = Path.GetDirectoryName(projectPath) + @"\";
            }

            string solutionPath = dte.Solution.FileName;
            if (solutionPath != "") // solutionPath will be empty if project isn't part of a saved solution
            {
               _solutionDirectory = Path.GetDirectoryName(solutionPath) + @"\";
            }
         }
      }
   }
}
