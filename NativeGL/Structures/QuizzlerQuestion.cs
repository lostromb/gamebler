using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeGL.Structures
{
    public class QuizzlerQuestion
    {
        public int Id;
        public string Category;
        public string QuestionText;
        public string Answer;
        public List<string> Incorrect;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Id == ((QuizzlerQuestion)obj).Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}
