using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
                        where line.Length > 0 && line.StartsWith("matrix-off") == false && line.StartsWith("noscript-spoof") == false
                        select new UMatrixRule(line);



            TreeNode<string, UMatrixRule> tree = new TreeNode<string, UMatrixRule>();
            Predicate<UMatrixRule>[] examptedFromRemoving =
            {
                //r=>r.ToString().Contains("#") || r.ToString()=="* * script block" || r.ToString()=="* * frame block",
                //r=>r.Source.Value=="*" && r.Destination.Value!="*" && (r.Type.HasFlag(DataType.Script) || r.Type.HasFlag(DataType.Frame)),
                r=>r.Destination=="simg.sinajs.cn"
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
            var currentRuleNode = rules.First;
            while (currentRuleNode != null)
            {
                var currentRule = currentRuleNode.Value;
                var generalizedRule = currentRule;
                bool isGeneralized = false;
                while (generalizedRule != null)
                {
                    var coveredRules = GetCoveredRules(currentRuleNode.Next, generalizedRule);

                    //如果GetClosestParent返回null，说明该规则不能再一般化，说明这是顶级规则，不能删除。
                    var toRemove = coveredRules.Where(r => GetClosestParent(r.Value, rules)?.IsAllow == r.Value.IsAllow).ToList();
                    if (isGeneralized == false)
                    {
                        foreach (var node in coveredRules)
                        {
                            if (examptedFromRemoving.Any(p => p(node.Value)))
                                continue;

                            var cp = GetClosestParent(node.Value, rules);
                            if (cp?.IsAllow == node.Value.IsAllow)
                            {
                                Console.WriteLine($"删除 \"{node.Value}\" ，因为它被 \"{cp}\" 包含。");
                                rules.Remove(node);
                            }
                        }
                    }
                    else if (toRemove.Count >= groupingThreshold)
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
                            Console.WriteLine("未能合并" + string.Join("\r\n\t", toRemove));
                            Console.WriteLine("==>\t为" + generalizedRule);
                            Console.WriteLine($"\t因为{conflictingRule}具有更高优先级");
                        }
                    }

                    generalizedRule = generalizedRule.Generalize();
                    isGeneralized = true;
                }


                currentRuleNode = currentRuleNode.Next;

            }
        }

        private static List<LinkedListNode<UMatrixRule>> GetCoveredRules(LinkedListNode<UMatrixRule> startNode, UMatrixRule generalizedRule)
        {
            List<LinkedListNode<UMatrixRule>> coveredRules = new List<LinkedListNode<UMatrixRule>>();
            while (startNode != null)
            {
                if (generalizedRule.Covers(startNode.Value).GetValueOrDefault(false) && generalizedRule.IsAllow == startNode.Value.IsAllow)
                    coveredRules.Add(startNode);

                startNode = startNode.Next;
            }

            return coveredRules;
        }

        /// <summary>
        /// 搜索list，查看是否有规则与指定的rule过滤器相同，但操作不同。
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static UMatrixRule FindConflictingRule(UMatrixRule rule, ICollection<UMatrixRule> list)
        {
            var closestParent = GetClosestParent(rule, list);
            if (closestParent?.IsAllow != rule.IsAllow)
                return closestParent;
            else
                return null;



            //UMatrixRule conflictingRule = null;
            //HierarchicalUrl parentDestination = rule.Destination.GetParent();
            //while (parentDestination != null)
            //{
            //    conflictingRule = list.FirstOrDefault(r =>
            //        r.Destination.Equals(parentDestination) && r.Covers(rule) &&
            //        r.IsAllow != rule.IsAllow);
            //    if (conflictingRule != null)
            //        break;

            //    parentDestination = parentDestination.GetParent();
            //}

            //return conflictingRule;
        }

        public static UMatrixRule GetClosestParent(UMatrixRule rule, ICollection<UMatrixRule> list)
        {
            var closestParent = list.Where(r => r.Equals(rule) == false && r.Covers(rule).GetValueOrDefault(true))
                                .OrderByDescending(r => r.Specificity).FirstOrDefault();
            return closestParent;
        }
    }


    class TreeNode<TKey, TValue>
    {
        public TKey Key { get; set; }

        /// <summary>
        /// 只有叶子节点才有Value
        /// </summary>
        public TValue Value { get; set; }

        public Dictionary<TKey, TreeNode<TKey, TValue>> Children { get; set; } = new Dictionary<TKey, TreeNode<TKey, TValue>>();
    }
}
