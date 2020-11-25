using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;

namespace XamarinApp.Extensions
{
    public static class VisualTreeHelper
    {
        private static void GetChildren<T>(Element element, List<T> childList) where T : Element
        {
            var children = element.LogicalChildren.Where(x => true).ToList();
            foreach (Element child in children)
            {
                if (child is T)
                    childList.Add((T)child);
                GetChildren<T>(child, childList);
            }
        }

        public static IList<T> GetChildren<T>(this Element element) where T : Element
        {
            List<T> children = new List<T>();
            GetChildren<T>(element, children);
            return children;
        }
    }
}