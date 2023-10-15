using System.Collections.ObjectModel;

namespace Common.Helpers
{
    public static class Extensions
    {
        public static ObservableCollection<T> AddRange<T>(this ObservableCollection<T> collection, List<T> list)
        {
            foreach (var item in list)
            {
                collection.Add(item);
            }

            return collection;
        }

        public static ObservableCollection<T> AddRange<T>(this ObservableCollection<T> collection, ObservableCollection<T> list)
        {
            foreach (var item in list)
            {
                collection.Add(item);
            }

            return collection;
        }

        public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
        {
            var element = list[oldIndex];
            list.RemoveAt(oldIndex);

            list.Insert(newIndex, element);
        }
    }
}
