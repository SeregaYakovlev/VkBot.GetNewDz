using System;
using System.Collections.Generic;
using System.Linq;
using static ClassLibrary.Global;

namespace Puppeteer
{
    public class Rootobject
    {
        public Data data { get; set; }

        internal IEnumerable<(Item old, Item @new)> GetDiffs(Rootobject newTree)
        {
            return data.GetDiffs(newTree.data);
        }
    }

    public class Data
    {
        public Item[] items { get; set; }

        internal IEnumerable<(Item, Item)> GetDiffs(Data data)
        {
            var join = items.FullOuterJoin(data.items, old => (old.subject_name, old.datetime_from), @new => (@new.subject_name, @new.datetime_from), (old, @new, key) => (old, @new));
            foreach (var joinEntry in join)
            {
                string dateTime = DateTime.Now.ToString(DateTimesFormats.FullDateTime);
                bool oldEmpty = (joinEntry.old == null);
                bool newEmpty = (joinEntry.@new == null);

                if (!oldEmpty) joinEntry.old.updateTime = dateTime;
                if (!newEmpty) joinEntry.@new.updateTime = dateTime;
                if (oldEmpty)
                {
                    joinEntry.@new.SubjectStatus = "new";
                    yield return joinEntry;
                }
                else if (newEmpty)
                {
                    joinEntry.old.SubjectStatus = "deleted";
                    yield return joinEntry;
                }
                else
                {
                    var diffs = joinEntry.old.GetDiffs(joinEntry.@new);
                    if (diffs.Any())
                    {
                        foreach (var diff in diffs)
                        {
                            if (diff.task_name.Any())
                            {
                                joinEntry.old.HomeworkStatus = "changed";
                                joinEntry.@new.HomeworkStatus = "changed";
                            };
                        }
                        yield return joinEntry;
                    }
                }
            }
        }
    }

    public class Item
    {
        public string datetime_from { get; set; }
        public string subject_name { get; set; }

        public string updateTime { get; set; }
        public string SubjectStatus { get; set; }
        public string HomeworkStatus { get; set; }
        public string content_name { get; set; }
        public Task[] tasks { get; set; }

        public IEnumerable<Task> GetDiffs(Item other)
        {
            if (!subject_name.Equals(other.subject_name) || !datetime_from.Equals(other.datetime_from))
            {
                throw new Exception("the subject_name or the datetime_from != the other.subject_name or the other.datetime_from");
            }
            string old = string.Join("", tasks.Select(t => t.task_name));
            string @new = string.Join("", other.tasks.Select(t => t.task_name));
            if (old != @new)
            {
                if (@new == "") return tasks;
                return other.tasks;
            }
            return Enumerable.Empty<Task>();
        }
    }

    public class Task
    {
        public string task_name { get; set; }

    }

    internal static class MyExtensions
    {
        internal static IEnumerable<TResult> FullOuterJoin<TA, TB, TKey, TResult>(
        this IEnumerable<TA> a,
        IEnumerable<TB> b,
        Func<TA, TKey> selectKeyA,
        Func<TB, TKey> selectKeyB,
        Func<TA, TB, TKey, TResult> projection,
        TA defaultA = default(TA),
        TB defaultB = default(TB),
        IEqualityComparer<TKey> cmp = null)
        {
            cmp = cmp ?? EqualityComparer<TKey>.Default;
            var alookup = a.ToLookup(selectKeyA, cmp);
            var blookup = b.ToLookup(selectKeyB, cmp);

            var keys = new HashSet<TKey>(alookup.Select(p => p.Key), cmp);
            keys.UnionWith(blookup.Select(p => p.Key));

            var join = from key in keys
                       from xa in alookup[key].DefaultIfEmpty(defaultA)
                       from xb in blookup[key].DefaultIfEmpty(defaultB)
                       select projection(xa, xb, key);

            return join;
        }
    }
}
