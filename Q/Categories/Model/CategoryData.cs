using NewKnowledgeAPI.Q.Questions.Model;
using System.Diagnostics.Metrics;
using System.Net;


namespace NewKnowledgeAPI.Q.Categories.Model
{
    public class CategoryData
    {
        public string? ParentCategory { get; set; }
        public string Id { get; set; }
        public string? PartitionKey { get; set; }
        public string Title { get; set; }
        public string? Link { get; set; }
        public string? Header { get; set; }
        public int Kind { get; set; }
        public int? Level { get; set; }
        public List<string>? Variations { get; set; }
        public List<CategoryData>? Categories { get; set; }
        public List<QuestionData>? Questions { get; set; }

        public void Deconstruct(
            out string? partitionKey,
            out string id,
            out string title,
            out string? link,
            out string? header,
            out string? parentCategory,
            out int kind,
            out int? level,
            out List<string>? variations,
            out List<CategoryData>? categories,
            out List<QuestionData>? questions)
            {
                partitionKey = PartitionKey;
                id = Id;
                title = Title;
                link = Link;
                header = Header;
                parentCategory = ParentCategory;
                kind = Kind;
                level = Level;
                variations = Variations;
                categories = Categories;
                questions = Questions;
            }
        }
    

    public class CategoriesData
    {
        public List<CategoryData> Categories { get; set; }
    }
}
