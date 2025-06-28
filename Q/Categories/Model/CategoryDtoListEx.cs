namespace NewKnowledgeAPI.Q.Categories.Model
{
 
    public class CategoryDtoListEx
    {
        public CategoryDtoListEx(CategoryListEx categoryListEx)
        {
            categoryDtoList = [];
            var (categoryList, msg) = categoryListEx;
            List<CategoryDto> list = [];
            if (categoryList != null)
            {
                foreach (var category in categoryList)
                {
                    var categoryDto = new CategoryDto(category);
                    categoryDtoList.Add(categoryDto);
                }
            }
            this.msg = msg;
        }

        public List<CategoryDto> categoryDtoList { get; set; }
        public string msg { get; set; }    
    }
}
