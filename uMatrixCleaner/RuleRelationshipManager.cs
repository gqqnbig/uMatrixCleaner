using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace uMatrixCleaner
{
	public class RuleRelationshipManager
	{
		private static readonly ILogger logger = ApplicationLogging.CreateLogger<RuleRelationshipManager>();

		private readonly ConcurrentDictionary<UMatrixRule, HashSet<UMatrixRule>> superOrJointRulesDictionary;
		private readonly IList<UMatrixRule> rules;

		/// <summary>
		/// 去除重复规则时，此事件被引发。
		/// </summary>
		public event EventHandler<DedupRuleEventArgs> DedupEvent;

		/// <summary>
		/// 合并规则时，此事件被引发。
		/// </summary>
		public event EventHandler<MergeEventArgs> MergeEvent;


		public RuleRelationshipManager(IList<UMatrixRule> rules)
		{
			this.rules = rules;
			superOrJointRulesDictionary = new ConcurrentDictionary<UMatrixRule, HashSet<UMatrixRule>>();

			Stopwatch sw = null;
			if (logger.IsEnabled(LogLevel.Debug))
			{
				logger.LogTrace("初始化{0}……", nameof(RuleRelationshipManager));
				sw = new Stopwatch();
				sw.Start();
			}

			Parallel.ForEach(rules, rule =>
			{
				var superRules = (from r in rules
								  where r.Selector.IsSuperOrHasJoint(rule.Selector)
								  select r).ToArray();

				superOrJointRulesDictionary.TryAdd(rule, new HashSet<UMatrixRule>(superRules));
			});

			if (logger.IsEnabled(LogLevel.Debug))
			{
				sw.Stop();
				logger.LogDebug("初始化{0}用时{1}毫秒", nameof(RuleRelationshipManager), sw.ElapsedMilliseconds);
			}
		}

		public ICollection<UMatrixRule> GetSuperOrJointRules(UMatrixRule rule)
		{
			return superOrJointRulesDictionary[rule];
		}

		private void RemoveFromRelationshipTable(UMatrixRule deletedRule)
		{
			superOrJointRulesDictionary.TryRemove(deletedRule, out _);

			foreach (var value in superOrJointRulesDictionary.Values)
			{
				value.Remove(deletedRule);
			}
		}

		private int savedSearch = 0;

		/// <summary>
		/// 返回新的规则集
		/// </summary>
		/// <param name="thresholdToRemove">如果为<see cref="int.MaxValue"/>则只去除重复，不进行合并。</param>
		/// <returns></returns>
		public List<UMatrixRule> Clean(int thresholdToRemove)
		{
			HashSet<UMatrixRule> processedRules = new HashSet<UMatrixRule>();
			savedSearch = 0;

			List<UMatrixRule> newRules = new List<UMatrixRule>();

			for (int i = 0; i < rules.Count; i++)
			{
				var currentRule = rules[i];


				var g = currentRule.Selector;
				bool isGeneralized = false;
				while (g != null)
				{
					var generalizedRule = new UMatrixRule(g.Source, g.Destination, g.Type, currentRule.IsAllow);
					if (processedRules.Contains(generalizedRule))
					{
						savedSearch++;
						break;
					}

					var subRules = (from r in rules
									where g.IsProperSuperOf(r.Selector) && r.IsAllow == currentRule.IsAllow
									select r).ToArray();


					if (subRules.Length >= (isGeneralized ? thresholdToRemove : 1))
					{
						var toRemove = new List<UMatrixRule>();
						foreach (var subRule in subRules)
						{
							var superOrJointRules = this.GetSuperOrJointRules(subRule).Union(newRules.Where(nr => nr.Selector.IsSuperOrHasJoint(subRule.Selector))).Where(r => r.IsAllow != subRule.IsAllow);
							if (superOrJointRules.Any(s => s.Priority > generalizedRule.Priority) ||
								generalizedRule.IsAllow && generalizedRule.Selector.Type == TypePredicate.All && subRule.Selector.Type != TypePredicate.All)
							{
								//不能删除
							}
							else
							{
								toRemove.Add(subRule);
							}
						}

						Debug.Assert(toRemove.Contains(currentRule) || toRemove.Contains(currentRule) == false,
							"当前规则可能被更高优先级规则锁定，但当前规则的推广规则可能可用于合并其他规则。");

						var groupings = toRemove.GroupBy(r => r.Selector.GetDistanceTo(generalizedRule.Selector)).ToArray();

						MergeEventArgs me = null;
						DedupRuleEventArgs de = null;
						foreach (var grouping in groupings)
						{
							if (grouping.Count() >= (isGeneralized ? GetDistance(grouping.Key) * thresholdToRemove : 1))
							{
								newRules.Add(generalizedRule);

								if (isGeneralized)
								{
									if (me == null)
										me = new MergeEventArgs();

									me.MasterRule = generalizedRule;
									me.RulesToDelete.AddRange(grouping);
								}
								else
								{
									if (de == null)
										de = new DedupRuleEventArgs();

									de.MasterRule = generalizedRule;
									de.DuplicateRules.AddRange(grouping);
								}

								for (int j = rules.Count - 1; j >= i; j--)
								{
									if (grouping.Contains(rules[j]))
									{
										RemoveFromRelationshipTable(rules[j]);
										rules[j] = rules[rules.Count - 1];
										rules.RemoveAt(rules.Count - 1);
									}
								}
							}
						}

						if (me != null)
							MergeEvent?.Invoke(this, me);
						if (de != null)
							DedupEvent?.Invoke(this, de);

						processedRules.Add(generalizedRule);

						break;
					}

					g = g.Generalize();
					isGeneralized = true;
					if (thresholdToRemove == Int32.MaxValue)
						break;
				}
			}

			logger.LogDebug("变量{0}节省了{1}次查询。", nameof(processedRules), savedSearch);

			foreach (var newRule in newRules)
			{
				if (rules.Contains(newRule) == false)
					rules.Add(newRule);
			}

			return rules.ToList();
		}

		public static byte GetDistance(Tuple<byte, byte, byte> distance)
		{
			if (distance.Item1 > 0)
				return distance.Item1;
			if (distance.Item2 > 0)
				return distance.Item2;
			return distance.Item3;
		}
	}


	public class DedupRuleEventArgs : EventArgs
	{
		public List<UMatrixRule> DuplicateRules { get; set; } = new List<UMatrixRule>();

		public UMatrixRule MasterRule { get; set; }

	}

	public class MergeEventArgs : EventArgs
	{
		public List<UMatrixRule> RulesToDelete { get; set; } = new List<UMatrixRule>();

		public UMatrixRule MasterRule { get; set; }
	}
}
