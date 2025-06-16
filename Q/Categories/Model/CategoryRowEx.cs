namespace NewKnowledgeAPI.Q.Categories.Model
{
 
    public class CategoryRowEx

    {
        public CategoryRowEx(CategoryRow? row, string msg)
        {
            categoryRow = row;
            message = msg;
        }

        public CategoryRowEx(Category? category, string msg)
        {
            categoryRow = category != null ? new CategoryRow(category) : null;
            message = msg;
        }


        public void Deconstruct(out CategoryRow? categoryRow, out string msg)
        {
            categoryRow = this.categoryRow;
            msg = this.message;
        }

        public CategoryRow? categoryRow { get; set; }
        public string message { get; set; }    
    }
}
