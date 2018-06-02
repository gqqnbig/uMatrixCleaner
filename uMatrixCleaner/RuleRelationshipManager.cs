using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Text;

namespace uMatrixCleaner
{
    class RuleRelationshipManager
    {
        private readonly ConcurrentDictionary<UMatrixRule, HashSet<UMatrixRule>> superOrJointRulesDictionary;
        private readonly Task initTask;


        public RuleRelationshipManager(UMatrixRule[] rules)
        {
            superOrJointRulesDictionary = new ConcurrentDictionary<UMatrixRule, HashSet<UMatrixRule>>();
            initTask = new Task(rs =>
            {
                Console.WriteLine($"初始化{nameof(RuleRelationshipManager)}……");
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                Parallel.ForEach(rules, rule =>
                {
                    var superRules = (from r in rules
                        where r.Selector.IsSuperOrHasJoint(rule.Selector)
                        select r).ToArray();

                    superOrJointRulesDictionary.TryAdd(rule, new HashSet<UMatrixRule>(superRules));
                });

                sw.Stop();
                Console.WriteLine($"初始化{nameof(RuleRelationshipManager)}用时{sw.ElapsedMilliseconds}毫秒");
            }, rules);
            initTask.Start();
        }

        public ICollection<UMatrixRule> GetSuperOrJointRules(UMatrixRule rule)
        {
            if (initTask.IsCompletedSuccessfully == false)
            {
                Console.WriteLine($"无法调用{nameof(GetSuperOrJointRules)}，等待{nameof(RuleRelationshipManager)}初始化");
                Task.WaitAll(initTask);
            }

            return superOrJointRulesDictionary[rule];
        }

        public void NotifyItemDeleted(UMatrixRule deletedRule)
        {
            if (initTask.IsCompletedSuccessfully == false)
            {
                Console.WriteLine($"无法调用{nameof(NotifyItemDeleted)}，等待{nameof(RuleRelationshipManager)}初始化");
                Task.WaitAll(initTask);
            }

            superOrJointRulesDictionary.TryRemove(deletedRule, out _);

            foreach (var value in superOrJointRulesDictionary.Values)
            {
                value.Remove(deletedRule);
            }
        }

        public void NotifyItemDeleted(IEnumerable<UMatrixRule> deletedRules)
        {
            if (initTask.IsCompletedSuccessfully == false)
            {
                Console.WriteLine($"无法调用{nameof(NotifyItemDeleted)}，等待{nameof(RuleRelationshipManager)}初始化");
                Task.WaitAll(initTask);
            }

            foreach (var deletedRule in deletedRules)
            {
                superOrJointRulesDictionary.TryRemove(deletedRule, out _);

                foreach (var value in superOrJointRulesDictionary.Values)
                {
                    value.Remove(deletedRule);
                }
            }
        }

        //private void Rules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        //{
        //    if (e.Action != NotifyCollectionChangedAction.Remove)
        //        throw new NotSupportedException($"{nameof(RuleRelationshipManager)}不支持监听{e.Action}操作。");
        //    throw new NotImplementedException();
        //}
    }
}
