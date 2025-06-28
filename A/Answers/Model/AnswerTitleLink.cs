using Newtonsoft.Json;
using System.Diagnostics.Metrics;


namespace NewKnowledgeAPI.A.Answers.Model
{
    public class AnswerTitleLink
    {
        public string? Id { get; set; }
        public string Title { get; set; }
        public string? Link { get; set; }

        public AnswerTitleLink()
        {
        }

        public AnswerTitleLink(AnswerTitleLink answerTitleLink)
        {
            this.Id = null;
            this.Title = answerTitleLink.Title;
            this.Link = answerTitleLink.Link;
        }

        public AnswerTitleLink(string title, string? link)
        {
            this.Id = null;
            this.Title = title;
            this.Link = link;
        }


    }

}
