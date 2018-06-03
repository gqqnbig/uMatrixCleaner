using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace uMatrixCleaner
{
    class RuleRelationshipManager
    {
        private static readonly ILogger logger =ApplicationLogging.CreateLogger<RuleRelationshipManager>();

        private readonly ConcurrentDictionary<UMatrixRule, HashSet<UMatrixRule>> superOrJointRulesDictionary;
        private readonly Task initTask;


        public RuleRelationshipManager(IList<UMatrixRule> rules)
        {
            superOrJointRulesDictionary = new ConcurrentDictionary<UMatrixRule, HashSet<UMatrixRule>>();
            initTask = new Task(rs =>
            {
                logger.LogTrace($"初始化{nameof(RuleRelationshipManager)}……");
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
                logger.LogDebug($"初始化{nameof(RuleRelationshipManager)}用时{sw.ElapsedMilliseconds}毫秒");
            }, rules);
            initTask.Start();
        }

        public ICollection<UMatrixRule> GetSuperOrJointRules(UMatrixRule rule)
        {
            if (initTask.IsCompletedSuccessfully == false)
            {
                logger.LogTrace($"无法调用{nameof(GetSuperOrJointRules)}，等待{nameof(RuleRelationshipManager)}初始化");
                Task.WaitAll(initTask);
            }

            return superOrJointRulesDictionary[rule];
        }

        public void NotifyItemDeleted(UMatrixRule deletedRule)
        {
            if (initTask.IsCompletedSuccessfully == false)
            {
                logger.LogTrace($"无法调用{nameof(NotifyItemDeleted)}，等待{nameof(RuleRelationshipManager)}初始化");
                Task.WaitAll(initTask);
            }

            superOrJointRulesDictionary.TryRemove(deletedRule, out _);

            foreach (var value in superOrJointRulesDictionary.Values)
            {
                value.Remove(deletedRule);
            }
        }
    }
}
