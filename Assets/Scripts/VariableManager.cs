using System.Collections.Generic;

[System.Serializable]
public class VariableManager
{
    private static VariableManager mInstance;

    public static VariableManager Instance
    {
        get
        {
            if (mInstance == null)
                mInstance = new VariableManager();
            return mInstance;
        }
    }

    public List<FlgVarData> var_flg = new List<FlgVarData>();
    public List<IntVarData> var_int = new List<IntVarData>();
    public List<StrVarData> var_str = new List<StrVarData>();
}

[System.Serializable]
public class VarData<T>
{
    public string name;
    public T var;
}

// ジェネリック型はシリアライズできないから仕方なく継承して型を固定
[System.Serializable]
public class FlgVarData : VarData<bool> { }
[System.Serializable]
public class IntVarData : VarData<int> { }
[System.Serializable]
public class StrVarData : VarData<string> { }