namespace Common.Axiom.Helpers;

public static class VersionComparer
{
    public static bool Compare(string? v1, string v2)
    {
        if (v1 is null)
        {
            return true;
        }

        string op;

        if (v2.StartsWith("=="))
        {
            v2 = v2[2..];
            op = "==";
        }
        else if (v2.StartsWith(">="))
        {
            v2 = v2[2..];
            op = ">=";
        }
        else if (v2.StartsWith("<="))
        {
            v2 = v2[2..];
            op = "<=";

        }
        else if (v2.StartsWith('>'))
        {
            v2 = v2[1..];
            op = ">";

        }
        else if (v2.StartsWith('<'))
        {
            v2 = v2[1..];
            op = "<";
        }
        else
        {
            op = "==";
        }

        return Compare(v1, v2, op);
    }

    public static bool Compare(string? v1, string v2, string op)
    {
        if (v1 is null)
        {
            return true;
        }

        List<int> expected = new(2);

        if (op.Equals("=="))
        {
            expected.Add(0);
        }
        else if (op.Equals(">="))
        {
            expected.Add(0);
            expected.Add(1);
        }
        else if (op.Equals("<="))
        {
            expected.Add(0);
            expected.Add(-1);

        }
        else if (op.Equals(">"))
        {
            expected.Add(1);

        }
        else if (op.Equals("<"))
        {
            expected.Add(-1);
        }
        else
        {
            throw new NotSupportedException();
        }

        var result = string.Compare(v1, v2);

        if (result > 0)
        {
            result = 1;
        }
        else if (result < 0)
        {
            result = -1;
        }

        return expected.Contains(result);
    }
}
