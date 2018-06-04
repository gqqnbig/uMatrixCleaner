using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace uMatrixCleaner
{
	public class Program
	{
		private static ILogger logger = ApplicationLogging.CreateLogger<Program>();

		static void Main(string[] args)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
			var configuration = builder.Build();

			ApplicationLogging.LoggerFactory.AddConsole(configuration.GetSection("Logging"));

			Clean(System.IO.File.ReadAllText(@"D:\Documents\Visual Studio 2017\Projects\uMatrixCleaner\test.txt"));

		}


		static void Clean(string input)
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
			//var workingRules = new LinkedList<UMatrixRule>(rules);
			//Deduplicate(workingRules, examptedFromRemoving);


			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			Merge(rules.ToList(), 2);
			sw.Stop();
			logger.LogDebug($"合并用时{sw.ElapsedMilliseconds}毫秒");


			//throw new NotImplementedException();
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

				if (mostDetailedSuperOrJointRules.Any(r => currentRule.Selector.HasJoint(r.Selector, out _, out _, out _) && r.IsAllow != currentRule.IsAllow))
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

		private static int savedSearch = 0;

		/// <summary>
		/// 返回新增的规则
		/// </summary>
		/// <param name="rules"></param>
		/// <param name="thresholdToRemove">如果为<see cref="int.MaxValue"/>则只去除重复，不进行合并。</param>
		/// <returns></returns>
		public static void Merge(List<UMatrixRule> rules, int thresholdToRemove)
		{
			HashSet<UMatrixRule> processedRules = new HashSet<UMatrixRule>();
			savedSearch = 0;

			var rrManager = new RuleRelationshipManager(rules);

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
							var superOrJointRules = rrManager.GetSuperOrJointRules(subRule).Where(r => r.IsAllow != subRule.IsAllow);
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

						foreach (var grouping in groupings)
						{
							if (grouping.Count() >= (isGeneralized ? GetDistance(grouping.Key) * thresholdToRemove : 1))
							{

								newRules.Add(generalizedRule); //新规则不再参与合并，否则会有叠加效应

								string info;
								if (isGeneralized)
								{
									info = "合并" + string.Join("、\r\n    ", grouping.Select(r => r.ToString("\t")));
									info += "\r\n  为" + generalizedRule.ToString("\t") + "。";
								}
								else
								{
									info = "删除    " + string.Join("、\r\n        ", grouping.Select(r => r.ToString("\t")));
									info += $"\r\n因为它被{generalizedRule.ToString("\t")}包含。";
								}


								for (int j = rules.Count - 1; j >= i; j--)
								{
									if (grouping.Contains(rules[j]))
									{
										rrManager.NotifyItemDeleted(rules[j]);
										rules[j] = rules[rules.Count - 1];
										rules.RemoveAt(rules.Count - 1);
									}
								}

								logger.LogInformation(info);
							}
						}

						processedRules.Add(generalizedRule);

						break;
					}

					g = g.Generalize();
					isGeneralized = true;
					if (thresholdToRemove == int.MaxValue)
						break;
				}
			}

			logger.LogDebug($"{nameof(processedRules)}变量节省了{savedSearch}次查询。");

			foreach (var newRule in newRules)
			{
				if (rules.Contains(newRule) == false)
					rules.Add(newRule);
			}
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


}
