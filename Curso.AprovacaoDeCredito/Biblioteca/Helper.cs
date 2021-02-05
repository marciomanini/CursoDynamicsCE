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

            //Tracing.Trace("Passei por aqui");

            //Evitar exceção Object reference not set to an instance of an object
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
                //Caso ocorra uma exceção em um dos 2 comandos, nenhum é executado
                Service.Update(contaUpdate);
                Service.Execute(request);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Falha ao alterar Status do Limite de crédito. Motivo: " + ex.Message + "\n");
            }
        }

        internal void ValidarConclusaoTarefa(EntityReference entidade, OptionSetValue state)
        {
            if (state.Value != 1) { return; }

            Entity tarefa = Service.Retrieve("curso_aprovacaodecredito", entidade.Id, new ColumnSet("curso_statusaprovacaolimite"));

            //Quando a entidade vem de uma busca, Retrieve por exemplo, somente o Contains é necessário, caso o registro não tenha o valor, ele não virá na entidade
            if (!tarefa.Contains("curso_statusaprovacaolimite") || ((OptionSetValue)tarefa["curso_statusaprovacaolimite"]).Value == (int)StatusLimite.Pendente)
            {
                //Para evitar exceção The given key was not present in the dictionary
                string nomeDoState = tarefa.Contains("curso_statusaprovacaolimite") ? tarefa.FormattedValues["curso_statusaprovacaolimite"].ToString() : "nulo";

                Tracing.Trace("O Status da aprovação é: " + nomeDoState);

                throw new InvalidPluginExecutionException("O campo Status Aprovação Limite precisa estar preenchido e diferente de pendente\n");
            }
        }

        internal void ValidarCriacao(Entity entityContext)
        {
            //Sempre desconfie que o campo está nulo
            if (!entityContext.Contains("regardingobjectid") || (entityContext.Contains("regardingobjectid") && entityContext["regardingobjectid"] == null)) { return; }
            if (!entityContext.Contains("curso_limite") || (entityContext.Contains("curso_limite") && entityContext["curso_limite"] == null)) { return; }

            QueryExpression query = new QueryExpression("curso_aprovacaodecredito");
            query.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, ((EntityReference)entityContext["regardingobjectid"]).Id);
            query.Criteria.AddCondition("curso_statusaprovacaolimite", ConditionOperator.Equal, (int)StatusLimite.Aprovado);
            query.Criteria.AddCondition("curso_limite", ConditionOperator.GreaterEqual, ((Money)entityContext["curso_limite"]).Value);
            //query.Criteria.AddCondition("curso_limite", ConditionOperator.NotNull);
            query.ColumnSet.AddColumn("curso_limite");
            query.ColumnSet.AddColumn("regardingobjectid");
            query.ColumnSet.AddColumn("subject");

            EntityCollection tarefas = Service.RetrieveMultiple(query);

            foreach (var tarefa in tarefas.Entities)
            {
                tarefa["subject"] = tarefa["subject"] + "-Depreciada";
                SetStateRequest request = new SetStateRequest
                {
                    EntityMoniker = tarefa.ToEntityReference(),
                    State = new OptionSetValue(0),//Status
                    Status = new OptionSetValue(1)//Razão de Status
                };

                Service.Execute(request);
                Service.Update(tarefa);
            }

            //if (tarefas.Entities.Count > 0)
            //{
            //    //decimal valorMaior = tarefas.Entities.Max(y => ((Money)y["curso_limite"]).Value);
            //    decimal valorMaior = tarefas.Entities.Where(x => x.Contains("curso_limite")).Max(y => ((Money)y["curso_limite"]).Value);
            //    throw new InvalidPluginExecutionException("Já existe uma tarefa de limite de crédito aprovada com valor de R$" + Decimal.Round(valorMaior, 2).ToString() + " para a conta " + ((EntityReference)tarefas.Entities.First()["regardingobjectid"]).Name);
            //}
        }
    }
}
