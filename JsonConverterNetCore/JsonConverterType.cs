using System;
using System.Collections.Generic;
using System.Text;

namespace JsonConverter
{
    class JsonConverterType
    {

        public String title;
        public String index;
        public String type;


        public JsonConverterType(String title, String index, String type)
        {
            this.title = title;
            this.index = index;
            this.type = type;

        }


        public string ToString()
        {

            return title + ";" + index + ";" + type;
        }
    }
}
