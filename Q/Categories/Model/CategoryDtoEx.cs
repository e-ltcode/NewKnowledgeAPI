using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using NewKnowledgeAPI.Common;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.Q.Categories.Model
{
       public class CategoryDtoEx
    {
        //public CategoryDtoEx(CategoryDto? categoryDto, string msg)
        //{
        //    this.categoryDto = categoryDto;
        //    this.msg = msg;
        //}
        public CategoryDtoEx(CategoryEx categoryEx)
        {
            categoryDto = categoryEx.category != null ? new CategoryDto(categoryEx.category!) : null;
            msg = categoryEx.msg!;
        }


        public CategoryDtoEx(CategoryDto categoryDto, string msg)
        {
            this.categoryDto = categoryDto;
            this.msg = msg;
        }

        public CategoryDtoEx(string msg)
        {
            categoryDto = null;
            this.msg = msg;
        }



        public CategoryDto? categoryDto { get; set; }
        public string msg { get; set; }
    }

}



