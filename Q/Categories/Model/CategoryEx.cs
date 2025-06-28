namespace NewKnowledgeAPI.Q.Categories.Model
{
 
    public class CategoryEx
    {
        public CategoryEx(Category? category, string msg)
        {
            this.category = category;
            this.msg = msg;
        }

        public void Deconstruct(out Category? category, out string msg)
        {
            category = this.category;
            msg = this.msg;
        }

        public Category? category { get; set; }
        public string msg { get; set; }    
    }
}
