

namespace Mochi.Event
{
    /// <summary>
    /// 可以在Unity编辑器中查看事件的信息
    /// </summary>
    public class EventInfoAttribute : System.Attribute
    {
        public string Description = "None";
        public bool Ignore = false;

        public EventInfoAttribute(string description, bool ignore = false)
        {
            Description = description;
            Ignore = ignore;
        }

        public EventInfoAttribute(bool ignore)
        {
            Ignore = ignore;
        }
    }
}
