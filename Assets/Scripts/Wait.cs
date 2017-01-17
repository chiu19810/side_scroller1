using UnityEngine;
using System.Collections;

public class Wait
{
    private int counter;
    private int maxCount;
    private bool isWait;

	public Wait()
    {
        init();
	}

    private void init()
    {
        counter = 0;
        maxCount = 0;
        isWait = true;
    }

    public bool SetWait(int count)
    {
        if (isWait)
        {
            maxCount = count;
            counter = 0;
            isWait = false;
        }
        else
        {
            if (counter > maxCount)
                return true;

            counter++;
        }

        return isWait;
    }

    public int SetToGetWait(int count)
    {
        if (isWait)
        {
            maxCount = count;
            counter = 0;
            isWait = false;
        }
        else
        {
            if (counter > maxCount)
                return count;

            counter++;
        }

        return counter;
    }

    public void Clear()
    {
        init();
    }
}
