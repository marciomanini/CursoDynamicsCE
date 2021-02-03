using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Curso.Conta.Biblioteca
{
    class Helper
    {
        #region Propriedades
        private IOrganizationService Service { get; set; }
        private IOrganizationService ServiceUsuario { get; set; }
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
        public Helper(IOrganizationService service, IOrganizationService serviceUsuario, IPluginExecutionContext context, ITracingService tracing)
        {
            Service = service;
            ServiceUsuario = serviceUsuario;
            Context = context;
            Tracing = tracing;
        }
        #endregion

        /// <summary>
        /// Cria Limites de Crédito para a conta
        /// </summary>
        /// <param name="entityContext"></param>
        internal void CriarLimiteDeCredito(Entity entityContext)
        {
            if (!entityContext.Contains("creditlimit") || entityContext["creditlimit"] == null)
            {
                entityContext["curso_statusaprovacaolimite"] = null;
                return;
            }

            if (((Money)entityContext["creditlimit"]).Value <= new decimal(10000))
            {
                entityContext["curso_statusaprovacaolimite"] = null;
                entityContext["curso_errovalidacao"] = null;
                return;
            }

            QueryExpression query = new QueryExpression("curso_aprovacaodecredito");
            query.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, entityContext.Id);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            query.Criteria.AddCondition("curso_statusaprovacaolimite", ConditionOperator.Equal, (int)StatusLimite.Pendente);
            query.ColumnSet.AddColumn("regardingobjectid");

            EntityCollection tarefas = Service.RetrieveMultiple(query);

            if (tarefas.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("Já existe um registro de Limite de Crédito pendente para a conta " + ((EntityReference)tarefas.Entities.First()["regardingobjectid"]).Name);
            }

            Entity usuario = Service.Retrieve("systemuser", Context.UserId, new ColumnSet("fullname"));

            Entity tarefaCreate = new Entity("curso_aprovacaodecredito");
            tarefaCreate["subject"] = "Aprovação de Limite em nome de " + usuario["fullname"].ToString();
            tarefaCreate["curso_limite"] = entityContext["creditlimit"];
            tarefaCreate["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Pendente);
            tarefaCreate["regardingobjectid"] = entityContext.ToEntityReference();

            //Service.Create(tarefaCreate);
            ServiceUsuario.Create(tarefaCreate);

            entityContext["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Pendente);
            entityContext["curso_errovalidacao"] = null;
        }

        /// <summary>
        /// Validar o Limite de Crédito em relação à Receita Anual
        /// </summary>
        /// <param name="entityContext"></param>
        internal void ValidarLimite(EntityReference entityContext)
        {
            //Mostrar exemplo de exceção não tratada
            //throw new InvalidPluginExecutionException("Erro não tratado");
            try
            {
                Entity conta = Service.Retrieve(entityContext.LogicalName, entityContext.Id, new ColumnSet("creditlimit", "revenue"));

                Entity contaUpdate = new Entity(entityContext.LogicalName, entityContext.Id);
                if (((Money)conta["creditlimit"]).Value > ((Money)conta["revenue"]).Value * new decimal(0.5))
                {
                    //conta["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Reprovado);//Errado!
                    contaUpdate["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Reprovado);//Correto
                    string mensagem = "Limite de Crédito não pode exceder o valor de R$" + Decimal.Round(((Money)conta["revenue"]).Value * new decimal(0.5), 2).ToString();
                    contaUpdate["curso_errovalidacao"] = mensagem;
                    Context.OutputParameters["retorno"] = mensagem;
                    //throw new InvalidPluginExecutionException(mensagem);//Mostrar o efeito
                }
                else
                {
                    contaUpdate["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Aprovado);
                }

                Service.Update(contaUpdate);
            }
            catch (Exception ex)
            {
                Context.OutputParameters["retorno"] = ex.Message;
            }
        }
    }
}
