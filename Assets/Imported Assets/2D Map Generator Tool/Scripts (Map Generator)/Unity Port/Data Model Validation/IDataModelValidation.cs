using System.Collections.Generic;

namespace MapGeneratorTool.UnityPort
{
    public interface IDataModelValidation
    {
        IEnumerable<ValidationError> Validate();
    }
}
