using System;
using System.Linq;
using System.Collections.Generic;

namespace uMatrixCleaner
{
    public class Program
    {
        static void Main(string[] args)
        {
            Clean(System.IO.File.ReadAllText(@"D:\Documents\Visual Studio 2017\Projects\uMatrixCleaner\test.txt"));

        }


        static string Clean(string input)
        {
            var rules = from line in input.Split("\r\n")
                        where line.Length > 0 && line.StartsWith("matrix-off") == false && line.StartsWith("noscript-spoof") == false && line.Contains("#") == false
                        select new UMatrixRule(line);



            Predicate<UMatrixRule>[] examptedFromRemoving =
            {
                //r=>r.ToString().Contains("#") || r.ToString()=="* * script block" || r.ToString()=="* * frame block",
                //r=>r.Source.Value=="*" && r.Destination.Value!="*" && (r.Type.HasFlag(TypePredicate.Script) || r.Type.HasFlag(TypePredicate.Frame)),
                r=>r.Destination.Value=="simg.sinajs.cn"
            };
            var workingRules = new LinkedList<UMatrixRule>(rules);
            Simply(workingRules, examptedFromRemoving, 3);


            ////合并Target属性
            //foreach (var rule in rules)
            //{
            //    var urlParts = rule.Destination.Split('.');
            //    TreeNode<string, UMatrixRule> node = tree;
            //    for (int i = urlParts.Length - 1; i >= 0; i--)
            //    {
            //        if (node.Children.TryGetValue(urlParts[i], out var child))
            //        {
            //            node = child;
            //        }
            //        else
            //        {
            //            child = new TreeNode<string, UMatrixRule>();
            //            child.Key = urlParts[i];
            //            if (i == 0)
            //                child.Value = rule;

            //            node.Children.Add(urlParts[i], child);
            //            node = child;
            //        }
            //    }
            //}



            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="examptedFromRemoving">符合任意谓词的规则不会被移除。特别适合解禁全局黑名单。</param>
        /// <param name="groupingThreshold"></param>
        static void Simply(LinkedList<UMatrixRule> rules, Predicate<UMatrixRule>[] examptedFromRemoving, int groupingThreshold)
        {
            LinkedListNode<UMatrixRule> nextNode = rules.First;
            while (nextNode != null)
            {
                var currentNode = nextNode;
                var currentRule = currentNode.Value;
                nextNode = currentNode.Next;
                //var generalizedRule = currentRule;

                if (examptedFromRemoving.Any(p => p(currentRule)))
                    continue;

                var coveringRules = GetCoveringRules(currentRule, rules).ToArray();
                if (coveringRules.Any(r => currentRule.Contains(r) && r.IsAllow != currentRule.IsAllow))
                {
                    //如果有一个部分匹配的相反规则，则本规则不能删除
                }
                else
                {
                    var cp = coveringRules.FirstOrDefault(r => currentRule.Contains(r) == false && r.IsAllow == currentRule.IsAllow);
                    //如果有一个完全匹配的相同规则，则本规则可以删除
                    if (cp != null)
                    {
                        Console.WriteLine($"删除 \"{currentRule}\" ，因为它被 \"{cp}\" 包含。");
                        rules.Remove(currentNode);
                    }
                }

            }
        }

        private static void MergeRules(LinkedList<UMatrixRule> rules, UMatrixRule generalizedRule, IEnumerable<LinkedListNode<UMatrixRule>> toRemove)
        {
            var conflictingRule = FindConflictingRule(generalizedRule, rules);

            if (conflictingRule == null)
            {
                Console.Write("合并");
                foreach (var node in toRemove)
                {
                    Console.WriteLine("\t" + node.Value);
                    rules.Remove(node);
                }

                Console.WriteLine("==>\t为" + generalizedRule);

                rules.AddFirst(generalizedRule);
            }
            else
            {
                Console.WriteLine("未能合并" + string.Join("\r\n\t", toRemove.Select(n => n.Value)));
                Console.WriteLine("==>\t为" + generalizedRule);
                Console.WriteLine($"\t因为{conflictingRule}具有更高优先级");
            }
        }

        /// <summary>
        /// 搜索list，查看是否有规则与指定的rule过滤器相同，但操作不同。
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static UMatrixRule FindConflictingRule(UMatrixRule rule, ICollection<UMatrixRule> list)
        {
            var closestParent = GetClosestParent(rule, list, true);
            if (closestParent?.IsAllow != rule.IsAllow)
                return closestParent;
            else
                return null;
        }

        /// <summary>
        /// 返回值以Specificity降序排列
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IEnumerable<UMatrixRule> GetCoveringRules(UMatrixRule rule, ICollection<UMatrixRule> list)
        {
            var ret = from r in list
                      where r.Equals(rule) == false && r.Contains(rule)
                      orderby r.Specificity descending
                      select r;

            return ret;
        }


        public static UMatrixRule GetClosestParent(UMatrixRule rule, ICollection<UMatrixRule> list, bool allowPartial)
        {
            var closestParent = list.Where(r => r.Equals(rule) == false && r.Contains(rule))
                                .OrderByDescending(r => r.Specificity).FirstOrDefault();
            return closestParent;
        }
    }
}
