using System.Collections.Generic;

public class WeightedRandomBag<T>
{
    private struct Entry
    {
        public double accumulatedWeight;
        public T item;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private double accumulatedWeight;
    private readonly System.Random rand = new System.Random();

    public void AddEntry(T item, double weight)
    {
        accumulatedWeight += weight;
        entries.Add(new Entry { item = item, accumulatedWeight = accumulatedWeight });
    }

    public T GetRandom()
    {
        double r = rand.NextDouble() * accumulatedWeight;
        for (int index = 0; index < entries.Count; index++)
        {
            Entry entry = entries[index];
            if (entry.accumulatedWeight >= r)
            {
                return entry.item;
            }
        }

        return default(T);
    }
    
    public List<T> GetRandom(int randomCnt)
    {
        List<T> randomList = new List<T>();
        if (randomCnt >= entries.Count)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                Entry entry = entries[index];
                randomList.Add(entry.item);
            }
        }
        else
        {
            HashSet<Entry> randomSet = new HashSet<Entry>();
            while (randomSet.Count < randomCnt)
            {
                double r = rand.NextDouble() * accumulatedWeight;
                for (int index = 0; index < entries.Count; index++)
                {
                    Entry entry = entries[index];
                    if (entry.accumulatedWeight >= r)
                    {
                        randomSet.Add(entry);
                        break;
                    }
                }
            }
            foreach (var entry in randomSet)
            {
                randomList.Add(entry.item);
            }
        }

        return randomList;
    }
}