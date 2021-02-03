using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Curso.AprovacaoDeCredito.Biblioteca
{
    class Helper
    {
        #region Propriedades
        private IOrganizationService Service { get; set; }
        private IPluginExecutionContext Context { get; set; }
        private ITracingService Tracing { get; set; }
        #endregion

        public enum StatusLimite
        {
            Pendente = 825660000,
            Aprovado = 825660001,
            Reprovado = 825660002
        }

        #region Contrutores
        public Helper(IOrganizationService service, IPluginExecutionContext context, ITracingService tracing)
        {
            Service = service;
            Context = context;
            Tracing = tracing;
        }
        #endregion

        internal void ConcluirTarefa(Entity entityContext, Entity preImage)
        {
            Entity contaUpdate = new Entity("account");
            SetStateRequest request = new SetStateRequest
            {
                EntityMoniker = entityContext.ToEntityReference(),
                State = new OptionSetValue(1),//Status
                Status = new OptionSetValue(2)//Razão de Status
            };

            if (entityContext["curso_statusaprovacaolimite"] == null || ((OptionSetValue)entityContext["curso_statusaprovacaolimite"]).Value == (int)StatusLimite.Pendente)
            {
                return;
            }

            //Garanto que se passar por aqui, o valor não será nulo
            if (!preImage.Contains("regardingobjectid"))
            {
                return;
            }

            contaUpdate.Id = ((EntityReference)preImage["regardingobjectid"]).Id;

            if (((OptionSetValue)entityContext["curso_statusaprovacaolimite"]).Value == (int)StatusLimite.Aprovado)
                contaUpdate["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Aprovado);
            else
                contaUpdate["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Reprovado);

            try
            {
                Service.Update(contaUpdate);
                Service.Execute(request);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Falha ao alterar Status do Limite de crédito. Motivo: " + ex.Message + "\n");
            }
        }

        internal void ValidarConclusaoTarefa(EntityReference entidade, OptionSetValue status)
        {
            if (status.Value != 1) { return; }

            Entity tarefa = Service.Retrieve("curso_aprovacaodecredito", entidade.Id, new ColumnSet("curso_statusaprovacaolimite"));

            if (!tarefa.Contains("curso_statusaprovacaolimite") || ((OptionSetValue)tarefa["curso_statusaprovacaolimite"]).Value == (int)StatusLimite.Pendente)
            {
                Tracing.Trace("O Status da aprovação é: " + tarefa.FormattedValues["curso_statusaprovacaolimite"].ToString());

                throw new InvalidPluginExecutionException("O campo Status Aprovação Limite precisa estar preenchido e diferente de pendente\n");
            }
        }

        internal void ValidarCriacao(Entity entityContext)
        {
            if (!entityContext.Contains("regardingobjectid") || (entityContext.Contains("regardingobjectid") && entityContext["regardingobjectid"] == null)) { return; }
            if (!entityContext.Contains("curso_limite") || (entityContext.Contains("curso_limite") && entityContext["curso_limite"] == null)) { return; }

            QueryExpression query = new QueryExpression("curso_aprovacaodecredito");
            query.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, ((EntityReference)entityContext["regardingobjectid"]).Id);
            query.Criteria.AddCondition("curso_statusaprovacaolimite", ConditionOperator.Equal, (int)StatusLimite.Aprovado);
            query.Criteria.AddCondition("curso_limite", ConditionOperator.GreaterEqual, ((Money)entityContext["curso_limite"]).Value);
            query.ColumnSet.AddColumn("curso_limite");
            query.ColumnSet.AddColumn("regardingobjectid");

            EntityCollection tarefas = Service.RetrieveMultiple(query);

            if (tarefas.Entities.Count > 0)
            {
                decimal valorMaior = tarefas.Entities.Where(x => x.Contains("curso_limite")).Max(y => ((Money)y["curso_limite"]).Value);
                throw new InvalidPluginExecutionException("Já existe uma tarefa de limite de crédito aprovada com valor de R$" + Decimal.Round(valorMaior, 2).ToString() + " para a conta " + ((EntityReference)tarefas.Entities.First()["regardingobjectid"]).Name);
            }
        }
    }
}
