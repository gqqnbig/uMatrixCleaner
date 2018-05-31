using System;
using System.Collections.Generic;
using System.Text;

namespace uMatrixCleaner
{
    internal static class LinkedListExtensions
    {

        //public static IEnumerable<T> EnumerateRest<T>(this LinkedListNode<T> node)
        //{
        //    while (node != null)
        //    {
        //        yield return node.Value;
        //        node = node.Next;
        //    }
        //}

        public static IEnumerable<LinkedListNode<T>> EnumerateNodes<T>(this LinkedList<T> list)
        {
            var node = list.First;
            while (node != null)
            {
                yield return node;
                node = node.Next;
            }
        }
    }
}
