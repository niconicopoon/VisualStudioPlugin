using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyBookmark
{
    class Util
    {
        public static int getLineNum(string s)
        {
            int len = s.Length;
            int idxNewLine = 0;
            int c = 0;
            for (int i = 0; i < len; i++)
            {
                if (s[i] == '\n')
                {
                    c++;
                    idxNewLine = i;
                }
            }

            if (idxNewLine == len - 1)
            {
                return c;       //最終行が空行なら改行数を返す
            }
            else
            {
                return c + 1;   //最終行に何かしら文字があれば、改行数に1行分足して返す
            }
        }
    }
}
