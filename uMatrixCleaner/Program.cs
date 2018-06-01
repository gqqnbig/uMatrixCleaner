using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

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
            //Deduplicate(workingRules, examptedFromRemoving);

            Merge(workingRules, 2);



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

                var mostDetailedSuperOrJointRules = GetMostDetailedSuperOrJointRules(currentRule, rules);

                if (mostDetailedSuperOrJointRules.Any(r => currentRule.Selector.HasJoint(r.Selector) && r.IsAllow != currentRule.IsAllow))
                {
                    //如果有一个部分匹配的相反规则，则本规则不能删除
                }
                else
                {
                    var cp = mostDetailedSuperOrJointRules.FirstOrDefault(s => s.Selector.IsSuperOf(currentRule.Selector) && s.IsAllow == currentRule.IsAllow);
                    //如果有一个完全匹配的相同规则，则本规则可以删除
                    if (cp != null)
                    {
                        Console.WriteLine($"删除 \"{currentRule}\" ，因为它被 \"{cp}\" 包含。");
                        rules.Remove(currentNode);
                    }
                }
            }
        }

        private static UMatrixRule[] GetMostDetailedSuperOrJointRules(UMatrixRule rule, IEnumerable<UMatrixRule> rules)
        {
            //superRules是包含当前规则的规则
            var superRules = (from r in rules
                              where r.Equals(rule) == false && r.Selector.IsSuperOrHasJoint(rule.Selector)
                              select r).ToArray();

            if (superRules.Length == 0)
                return Array.Empty<UMatrixRule>();//不用new UMatrixRule[0]因为Array.Empty()会重用对象。

            var highestSpecificity = superRules.Max(r => r.Selector.Specificity);
            var mostDetailedSuperOrJointRules = superRules.Where(r => r.Selector.Specificity == highestSpecificity);
            return mostDetailedSuperOrJointRules.ToArray();
        }

        public static void Merge(LinkedList<UMatrixRule> rules, int thresholdToRemove)
        {
            HashSet<UMatrixRule> notWorkingRules = new HashSet<UMatrixRule>();
            List<UMatrixRule> newRules = new List<UMatrixRule>();

            LinkedListNode<UMatrixRule> nextNode = rules.First;
            while (nextNode != null)
            {
                var currentNode = nextNode;
                var currentRule = currentNode.Value;
                nextNode = currentNode.Next;


                var g = currentRule.Selector.Generalize();
                while (g != null)
                {
                    var generalizedRule = new UMatrixRule(g.Source, g.Destination, g.Type, currentRule.IsAllow);
                    if (notWorkingRules.Contains(generalizedRule))
                        break;

                    LinkedListNode<UMatrixRule>[] subRules = null;
                    if ((g.Source.IsDomain || g.Source.IsIP) && g.Destination.IsDomain && g.Type != TypePredicate.All)
                    {
                        subRules = (from r in rules.EnumerateNodes()
                                    where g.IsProperSuperOf(r.Value.Selector) && r.Value.IsAllow == currentRule.IsAllow
                                    select r).ToArray();
                    }


                    if (subRules?.Length >= thresholdToRemove)
                    {
                        var toRemove = new List<LinkedListNode<UMatrixRule>>();
                        foreach (var subRule in subRules)
                        {
                            var superOrJointRules = GetHighestPrioritySuperOrJointRules(subRule.Value, rules);

                            if (superOrJointRules.Any(s => s.Priority > generalizedRule.Priority))
                            {
                                //不能删除
                            }
                            else
                            {
                                toRemove.Add(subRule);
                            }
                        }


                        if (toRemove.Count >= thresholdToRemove)
                        {
                            Debug.Assert(toRemove.Contains(currentNode));

                            newRules.Add(generalizedRule);//新规则不再参与合并，否则会有叠加效应

                            while (nextNode != null && toRemove.Contains(nextNode))
                                nextNode = nextNode.Next;

                            Console.Write("合并");
                            foreach (var node in toRemove)
                            {
                                Console.WriteLine("\t\t" + node.Value);
                                rules.Remove(node);
                            }

                            Console.WriteLine("\t为" + generalizedRule);

                        }

                        break;
                    }

                    g = g.Generalize();
                }
            }

            foreach (var newRule in newRules)
                rules.AddLast(newRule);
        }

        private static UMatrixRule[] GetHighestPrioritySuperOrJointRules(UMatrixRule rule, IEnumerable<UMatrixRule> rules)
        {
            //superRules是包含当前规则的规则
            var superRules = (from r in rules
                where r.Equals(rule) == false && r.Selector.IsSuperOrHasJoint(rule.Selector)
                select r).ToArray();

            if (superRules.Length == 0)
                return Array.Empty<UMatrixRule>();//不用new UMatrixRule[0]因为Array.Empty()会重用对象。

            var priority = superRules.Max(r => r.Priority);
            var highestPrioritySuperOrJointRules = superRules.Where(r => r.Priority == priority);
            return highestPrioritySuperOrJointRules.ToArray();
        }
    }
}
