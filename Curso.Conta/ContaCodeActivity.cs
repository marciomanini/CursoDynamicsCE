using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Curso.Conta
{
    public class ContaCodeActivity : CodeActivity
    {
        [RequiredArgument]
        [Input("Conta")]
        [ReferenceTarget("account")]
        public InArgument<EntityReference> ContaRef { get; set; }

        private IOrganizationService Service;

        public enum StatusLimite
        {
            Pendente = 825660000,
            Aprovado = 825660001,
            Reprovado = 825660002
        }

        protected override void Execute(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null) { return; }
            if (ContaRef.Get(executionContext) == null) { return; }

            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            Service = serviceFactory.CreateOrganizationService(null);

            Entity conta = Service.Retrieve("account", ContaRef.Get(executionContext).Id, new Microsoft.Xrm.Sdk.Query.ColumnSet("revenue", "creditlimit"));

            if (!conta.Contains("revenue")) { return; }
            if (!conta.Contains("creditlimit")) { return; }

            Entity contaUpdate = new Entity(conta.LogicalName)
            {
                Id = conta.Id
            };
            if (((Money)conta["creditlimit"]).Value > ((Money)conta["revenue"]).Value * new decimal(0.5))
            {
                contaUpdate["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Reprovado);
                contaUpdate["curso_errovalidacao"] = string.Format(
                    "O valor do limite de crédito não pode ser maior que 50% da Receita Anual\n50% Receita: R${0}\nLimite de Crédito: R${1}",
                    Decimal.Round(((Money)conta["revenue"]).Value * new decimal(0.5), 2).ToString(),
                    Decimal.Round(((Money)conta["creditlimit"]).Value, 2).ToString()
                );
            }

            Service.Update(contaUpdate);
        }
    }
}
