using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.Q.Categories.Model
{
       public class CategoryRowDtoEx
    {
        //public CategoryDtoEx(CategoryDto? categoryDto, string msg)
        //{
        //    this.categoryDto = categoryDto;
        //    this.msg = msg;
        //}
        public CategoryRowDtoEx(CategoryRowEx categoryRowEx)
        {

            categoryRowDto = categoryRowEx.categoryRow != null ? new CategoryRowDto(categoryRowEx.categoryRow!) : null;
            msg = categoryRowEx.message!;
        }


        public CategoryRowDtoEx(CategoryRowDto categoryRowDto, string msg)
        {
            this.categoryRowDto = categoryRowDto;
            this.msg = msg;
        }

        public CategoryRowDtoEx(string msg)
        {
            categoryRowDto = null;
            this.msg = msg;
        }


        public CategoryRowDto? categoryRowDto { get; set; }
        public string msg { get; set; }
    }

}



