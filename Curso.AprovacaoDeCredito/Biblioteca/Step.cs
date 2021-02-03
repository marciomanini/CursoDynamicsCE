using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Curso.AprovacaoDeCredito.Biblioteca
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
        PreValidation = 10,
        PreOperation = 20,
        PostOperation = 40
    }

    public enum StepExecutionMode
    {
        Synchronous = 0,
        Asynchronous = 1
    }
}
