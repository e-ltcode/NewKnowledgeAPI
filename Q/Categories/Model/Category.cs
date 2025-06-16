using Microsoft.AspNetCore.OutputCaching;
using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System;

namespace NewKnowledgeAPI.Q.Categories.Model
{
    public class Category : Record, IDisposable
    {
        public string Type { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        public string PartitionKey { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "RootId", NullValueHandling = NullValueHandling.Ignore)]
        public string? RootId { get; set; }

        public string? ParentCategory { get; set; }

        public string Title { get; set; }
        public string? Link { get; set; }
        public string Header { get; set; }

        public int Kind { get; set; }
        public int Level { get; set; }
        public List<string>? Variations { get; set; }
        public int NumOfQuestions { get; set; }
        public bool HasSubCategories { get; set; }

        [JsonProperty(PropertyName = "SubCategories", NullValueHandling = NullValueHandling.Ignore)]
        //public List<Category>? SubCategories {  get; set; }
        public List<CategoryRow>? SubCategories { get; set; }

        [JsonProperty(PropertyName = "Questions", NullValueHandling = NullValueHandling.Ignore)]
        public List<QuestionRow>? QuestionRows { get; set; }

        [JsonProperty(PropertyName = "HasMoreQuestions", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasMoreQuestions { get; set; }

        [JsonProperty(PropertyName = "IsExpanded", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsExpanded { get; set; }


        public Category()
            : base()
        {
        }

        public Category(Question question)
          : base()
        {
            Id = question.ParentCategory!;
            PartitionKey = question.PartitionKey;
        }


        public Category(CategoryData categoryData)
            : base(new WhoWhen("Admin"), null, null)
        {
            var (partitionKey, id, title, link, header, parentCategory, kind, level, variations, categories, questions) = categoryData;

            Type = "category";
            Id = id;
            PartitionKey = partitionKey ?? categoryData.Id;
            Title = title;
            Link = link;
            Header = header ?? ""; 
            Kind = kind;
            ParentCategory = parentCategory;
            Level = (int)level!;
            Variations = variations ?? null;
            NumOfQuestions = questions == null ? 0 : questions.Count;
            HasSubCategories = categories != null && categories.Count > 0;
            QuestionRows = null;
        }

        public Category(CategoryDto categoryDto)
            :base(categoryDto.Created, categoryDto.Modified, null)
        {
            Type = "category";
            Id = categoryDto.Id;
            PartitionKey = categoryDto.PartitionKey ?? categoryDto.Id;
            Title = categoryDto.Title;
            Link = categoryDto.Link;
            Kind = categoryDto.Kind;
            ParentCategory = categoryDto.ParentCategory;
            Level = categoryDto.Level;
            Variations = categoryDto.Variations ?? null;
            QuestionRows = null;
            NumOfQuestions = 0;
            HasSubCategories = false;
        }

        //public Category(Category category)
        //   : base(category.Created, category.Modified, null)
        //{
        //    return JsonConvert.DeserializeObject(JsonConvert.SerializeObject(category));
        //}

        //public override string ToString() =>
        //    $"{PartitionKey}/{Id} : {Title}";


        public void Deconstruct(
            out string partitionKey,
            out string id, 
            out string parentCategory, 
            out string title,
            out string? link,
            out string header,
            out int level, 
            out int kind,
            out bool hasSubCategories,
            //out List<Category> subCategories,
            out List<CategoryRow> subCategories,
            out bool? hasMoreQuestions,
            out int numOfQuestions,
            out List<QuestionRow>? questionRows,
            out List<string>? variations,
            out bool? isExpanded,
            out string? rootId)
        {
            partitionKey = PartitionKey;
            id = Id;
            parentCategory = ParentCategory;
            title = Title;
            link = Link;
            header = Header;
            kind = Kind;
            level = Level;
            hasSubCategories = HasSubCategories;
            subCategories = SubCategories ?? [];
            numOfQuestions = NumOfQuestions;
            questionRows = QuestionRows;
            hasMoreQuestions = HasMoreQuestions;
            variations = Variations;
            isExpanded = IsExpanded;
            rootId = RootId;
        }

        public static int Comparer(Category x, Category y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal.
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater.
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {
                    // ...and y is not null, compare the
                    // lengths of the two strings.
                    //
                    int retval = x.Title.CompareTo(y.Title);  // ASC
                    return retval;
                }
            }
        }

        public Category ShallowCopy()
        {
            return (Category)MemberwiseClone();
        }

        public Category DeepCopy()
        {
            return JsonConvert.DeserializeObject<Category>(JsonConvert.SerializeObject(this))!;
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



