using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Curso.Conta.Biblioteca
{
    public enum StepMessage
    {
        update,
        create,
        delete,
        assing,
        setstate,
        setstatedynamicentity
    }

    public enum StepEventStage
    {
        PreValidation = 10,//Nâo preciso dar update
        PreOperation = 20,//Nâo preciso dar update
        PostOperation = 40//Preciso dar update - É o único que pode ser assíncrono
    }

    public enum StepExecutionMode
    {
        Synchronous = 0,
        Asynchronous = 1
    }
}
