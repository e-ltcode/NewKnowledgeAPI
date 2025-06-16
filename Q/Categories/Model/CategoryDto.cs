using NewKnowledgeAPI.Common;
using NewKnowledgeAPI.Q.Questions.Model;
using Newtonsoft.Json;
using System.Net;

namespace NewKnowledgeAPI.Q.Categories.Model
{
    public class CategoryDto : RecordDto
    {
        public string? PartitionKey { get; set; }

        [JsonProperty(PropertyName = "Id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "PartitionKey")]
        public string? RootId { get; set; }
        public string? ParentCategory { get; set; }


        public string Title { get; set; }
        public string? Link { get; set; }
        public string Header { get; set; }

        public int Kind { get; set; }
        public int Level { get; set; }
        public List<string>? Variations { get; set; }
        public bool HasSubCategories { get; set; }
        public List<CategoryRowDto>? SubCategories { get; set; }
        public int NumOfQuestions { get; set; }
        public List<QuestionRowDto>? QuestionRowDtos { get; set; }
        public bool? HasMoreQuestions { get; set; }

        public bool? IsExpanded { get; set; }
       

        public CategoryDto()
            : base()
        {
        }
      

        public CategoryDto(CategoryKey categoryKey, QuestionsMore questionsMore)
            : base() // TODO
            //: base(null, null, null) // TODO prosledi 
        {
            var (partitionKey, id) = categoryKey;
            Id = id;
            PartitionKey = partitionKey;
            Title = "deca";
            Link = null;
            Header = "peca";
            Kind = 1;
            Level = 1;
            Variations = [];

            //Console.WriteLine("pitanja {0}", questionsMore.questions.Count);
            //if (questionsMore.questions.Count > 0) {
            //    Question q = questionsMore.questions.First();
            //}
            QuestionRowDtos = Questions2Dto(questionsMore.QuestionRows/*.Select(row => new Question(row))*/.ToList());
            HasMoreQuestions = questionsMore.HasMoreQuestions;
        }

        public CategoryDto(Category category)
            : base(category.Created, category.Modified)
        {
            var (partitionKey, id, parentCategory, title, link, header, level, kind,
                hasSubCategories, subCategories,
                hasMoreQuestions, numOfQuestions, questionRows, variations, isExpanded, rootId) = category;

            Id = id;
            PartitionKey = partitionKey!;
            Title = title;
            Link = link;
            Header = header;   
            Kind = kind;
            RootId = rootId;
            ParentCategory = parentCategory;
            Level = level;
            Variations = variations;
            HasSubCategories = hasSubCategories;
            SubCategories = subCategories != null 
                ? subCategories.Select(row => new CategoryRowDto(row)).ToList()
                : [];
            NumOfQuestions = numOfQuestions; //questions == null ? 0 : questions.Count;
            IsExpanded = isExpanded;
            if (questionRows == null)
            {
                QuestionRowDtos = [];
                HasMoreQuestions = false;
            }
            else
            {
                //IList<QuestionDto> questions = new List<QuestionDto>();
                //foreach (var question in category.questions)
                //    questions.Add(new QuestionDto(question));
                QuestionRowDtos = Questions2Dto(questionRows!);
                HasMoreQuestions = hasMoreQuestions;
            }
        }

        public List<QuestionRowDto> Questions2Dto(List<QuestionRow> questionRows)
        {
            List<QuestionRowDto> list = [];
            foreach (var questionRow in questionRows)
            {
                //Console.WriteLine(JsonConvert.SerializeObject(question));
                list.Add(new QuestionRowDto(questionRow));
            }
            return list;
        }

        public List<QuestionDto> Questions2Dto(List<Question> questions)
        {
            List<QuestionDto> list = [];
            foreach (var question in questions)
            {
                //Console.WriteLine(JsonConvert.SerializeObject(question));
                list.Add(new QuestionDto(question));
            }
            return list;
        }

        public void Deconstruct(out string partitionKey, out string id)
        {
            partitionKey = PartitionKey;
            id = Id;
        }

        public void Deconstruct(out string partitionKey, out string id, out string parentCategory, 
                out string title, out string? link, 
                out int level, out int kind, out List<string>? variations,
                out WhoWhenDto? modified)
        {
            partitionKey = PartitionKey;
            id = Id;
            parentCategory = ParentCategory;
            title = Title;
            link = Link;
            kind = Kind;
            level = Level;
            variations = Variations;
            modified = Modified;
        }

    }
}



