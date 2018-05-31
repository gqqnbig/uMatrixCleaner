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
                r=>r.Selector.Destination.Value=="simg.sinajs.cn"
            };
            var workingRules = new LinkedList<UMatrixRule>(rules);
            Deduplicate(workingRules, examptedFromRemoving);

            //Merge(workingRules, 3);



            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="examptedFromRemoving">符合任意谓词的规则不会被移除。特别适合解禁全局黑名单。</param>
        public static void Deduplicate(LinkedList<UMatrixRule> rules, Predicate<UMatrixRule>[] examptedFromRemoving)
        {
            LinkedListNode<UMatrixRule> nextNode = rules.First;
            while (nextNode != null)
            {
                var currentNode = nextNode;
                var currentRule = currentNode.Value;
                nextNode = currentNode.Next; //保存下节点指针，万一本节点被删除了。
                //var generalizedRule = currentRule;

                if (examptedFromRemoving.Any(p => p(currentRule)))
                    continue;

                var mostDetailedSuperRules = GetMostDetailedSuperRules(currentRule, rules);

                if (mostDetailedSuperRules.Any(r => currentRule.Selector.HasJoint(r.Selector) && r.IsAllow != currentRule.IsAllow))
                {
                    //如果有一个部分匹配的相反规则，则本规则不能删除
                }
                else
                {
                    var cp = mostDetailedSuperRules.FirstOrDefault(s => s.Selector.IsSuperOf(currentRule.Selector) && s.IsAllow == currentRule.IsAllow);
                    //如果有一个完全匹配的相同规则，则本规则可以删除
                    if (cp != null)
                    {
                        Console.WriteLine($"删除 \"{currentRule}\" ，因为它被 \"{cp}\" 包含。");
                        rules.Remove(currentNode);
                    }
                }
            }
        }

        private static UMatrixRule[] GetMostDetailedSuperRules(UMatrixRule rule, IEnumerable<UMatrixRule> rules)
        {
            //superRules是包含当前规则的规则
            var superRules = (from r in rules
                              where r.Equals(rule) == false && r.Selector.IsSuperOrHasJoint(rule.Selector)
                              select r).ToArray();

            if (superRules.Length == 0)
                return Array.Empty<UMatrixRule>();//不用new UMatrixRule[0]因为Array.Empty()会重用对象。

            var highestSpecificity = superRules.Max(r => r.Selector.Specificity);
            var mostDetailedSuperRules = superRules.Where(r => r.Selector.Specificity == highestSpecificity);
            return mostDetailedSuperRules.ToArray();
        }

        //private static void Merge(LinkedList<UMatrixRule> rules)
        //{
        //    LinkedListNode<UMatrixRule> nextNode = rules.First;
        //    while (nextNode != null)
        //    {
        //        var currentNode = nextNode;
        //        var currentRule = currentNode.Value;
        //        nextNode = currentNode.Next;


        //        var generalizedRule = currentRule.Generalize();
        //        while (generalizedRule != null)
        //        {
        //            var subsetRules = from r in rules
        //                              where generalizedRule.IsProperSuperOf(r) && r.IsAllow == generalizedRule.IsAllow
        //                              select r;

        //            foreach (var subsetRule in subsetRules)
        //            {
        //                var superRules = rules.Any(r => r.IsSuperOrHasJoint(subsetRule));
        //            }

        //            var coveredRules = GetCoveredRules(currentRuleNode.Next, generalizedRule);

        //            //如果GetClosestParent返回null，说明该规则不能再一般化，说明这是顶级规则，不能删除。
        //            var toRemove = coveredRules.Where(r => GetClosestParent(r.Value, rules, false)?.IsAllow == r.Value.IsAllow && examptedFromRemoving.Any(p => p(r.Value)) == false).ToList();
        //            if (toRemove.Count >= groupingThreshold)
        //            {
        //                Merge(rules, generalizedRule, toRemove);
        //            }

        //            generalizedRule = generalizedRule.Generalize();
        //            isGeneralized = true;
        //        }
        //    }


        //    var conflictingRule = FindConflictingRule(generalizedRule, rules);

        //    if (conflictingRule == null)
        //    {
        //        Console.Write("合并");
        //        foreach (var node in toRemove)
        //        {
        //            Console.WriteLine("\t" + node.Value);
        //            rules.Remove(node);
        //        }

        //        Console.WriteLine("==>\t为" + generalizedRule);

        //        rules.AddFirst(generalizedRule);
        //    }
        //    else
        //    {
        //        Console.WriteLine("未能合并" + string.Join("\r\n\t", toRemove.Select(n => n.Value)));
        //        Console.WriteLine("==>\t为" + generalizedRule);
        //        Console.WriteLine($"\t因为{conflictingRule}具有更高优先级");
        //    }
        //}



        public static UMatrixRule GetClosestParent(UMatrixRule rule, ICollection<UMatrixRule> list, bool allowPartial)
        {
            var closestParent = list.Where(r => r.Equals(rule) == false && r.Selector.IsSuperOrHasJoint(rule.Selector))
                                .OrderByDescending(r => r.Selector.Specificity).FirstOrDefault();
            return closestParent;
        }
    }
}
