﻿using System;

namespace MyBookmark
{
   internal static class ExceptionHandler
   {
      public static void Notify(Exception ex, bool showMessage)
      {
         string message = $"{DateTime.Now}: {ex}";
         Console.WriteLine(message);
         if (showMessage)
         {
            UIMessage.Show(message);
         }
      }
   }
}
