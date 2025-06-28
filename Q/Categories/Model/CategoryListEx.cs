namespace NewKnowledgeAPI.Q.Categories.Model
{
 
    public class CategoryListEx
    {
        public CategoryListEx(List<Category>? categoryList, string msg)
        {
            CategoryList = categoryList;
            Msg = msg;
        }

        public void Deconstruct(out List<Category>? categoryList, out string msg)
        {
            categoryList = CategoryList;
            msg = Msg;
        }


        public List<Category>? CategoryList { get; set; }
        public string Msg { get; set; }    
    }
}
