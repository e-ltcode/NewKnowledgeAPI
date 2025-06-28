using Microsoft.AspNetCore.OutputCaching;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System;

namespace NewKnowledgeAPI.Q.Categories.Model
{
    public class CatDto: IDisposable
    {
        public string PartitionKey { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }

        public int Kind { get; set; }
        public string ParentCategory { get; set; }
        public int Level { get; set; }
        public int NumOfQuestions { get; set; }
        public bool HasSubCategories { get; set; }
        public string? Link { get; set; }
        public string Header { get; set; }
        public List<string>? Variations { get; set; }

        public CatDto(Category category)
        {
            var (partitionKey, id, parentCategory, title, link, header, level, kind,
                hasSubCategories, _,
                hasMoreQuestions, numOfQuestions, _, variations, _, rootId) = category;
            Id = id;
            PartitionKey = partitionKey;
            Title = title;
            Kind = kind;
            ParentCategory = parentCategory;
            Level = level;
            NumOfQuestions = numOfQuestions;
            HasSubCategories = hasSubCategories;
            Variations = variations ?? [];
            Link = link;
            Header = header;
        }
        

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

    }
}



