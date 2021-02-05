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
            //throw new InvalidPluginExecutionException("Erro feio");

            //Quando vem do contexto, o valor pode vir nulo, diferentemente de quando vem de uma busca na base de dados
            if (!entityContext.Contains("creditlimit") || entityContext["creditlimit"] == null)
            {
                entityContext["curso_statusaprovacaolimite"] = null;
                entityContext["curso_errovalidacao"] = null;
                //Não preciso dar update porque a transação com o banco não foi efetuada
                return;
            }

            if (((Money)entityContext["creditlimit"]).Value <= new decimal(10000))
            {
                entityContext["curso_statusaprovacaolimite"] = null;
                entityContext["curso_errovalidacao"] = null;
                return;
            }

            QueryExpression query = new QueryExpression("curso_aprovacaodecredito");
            query.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, entityContext.Id);//Sempre que pesquisar um Lookup, passo o Id (Guid)
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);//Status (StateCode) em Rascunho
            query.Criteria.AddCondition("curso_statusaprovacaolimite", ConditionOperator.Equal, (int)StatusLimite.Pendente);//Campo OptionSetValue, vulgo ComboBox, passo um valor inteiro
            query.ColumnSet.AddColumn("regardingobjectid");

            /*
            string query2 = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='curso_aprovacaodecredito'>
                                <attribute name='activityid' />
                                <attribute name='subject' />
                                <attribute name='createdon' />
                                <order attribute='subject' descending='false' />
                                <filter type='and'>
                                  <condition attribute='regardingobjectid' operator='eq' uiname='Adventure Works (exemplo)' uitype='account' value='" + entityContext.Id.ToString() + @"' />
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <condition attribute='curso_statusaprovacaolimite' operator='eq' value='825660000' />
                                </filter>
                              </entity>
                            </fetch>";

            EntityCollection collection = Service.RetrieveMultiple(new FetchExpression(query2));
            */

            //Utilizei o RetrieveMultiple. Usado para trazer listas de registros
            EntityCollection tarefas = Service.RetrieveMultiple(query);

            if (tarefas.Entities.Count > 0)
            {
                throw new InvalidPluginExecutionException("Já existe um registro de Limite de Crédito pendente para a conta " + ((EntityReference)tarefas.Entities.First()["regardingobjectid"]).Name);
            }

            //Utilizei o Retrieve. Usado para trazer um registro. Busca por Id (Guid)
            Entity usuario = Service.Retrieve("systemuser", Context.UserId, new ColumnSet("fullname", "domainname"));

            Entity tarefaCreate = new Entity("curso_aprovacaodecredito");
            //tarefaCreate["subject"] = "Aprovação de Limite em nome de " + usuario["fullname"].ToString();
            tarefaCreate["subject"] = "Aprovação de Limite" + (usuario.Contains("fullname") ? " em nome de" + usuario["fullname"].ToString() : string.Empty);
            tarefaCreate["curso_limite"] = entityContext["creditlimit"];
            tarefaCreate["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Pendente);
            /*
             * Se não enviar o valor, quando eu verificar, vai dar false no Contais, se eu enviar nulo, vai dar true no Contais e o valor vai estar nulo
            */
            tarefaCreate["regardingobjectid"] = entityContext.ToEntityReference();//Lookup recebe um EntityReference;
            //tarefaCreate["regardingobjectid"] = null;
            /**********************/

            //Service.Create(tarefaCreate);//Vai Criar para o Administrador
            ServiceUsuario.Create(tarefaCreate);//Vai criar para o usuário que estiver executando a ação

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
                if (!conta.Contains("creditlimit")) { return; }
                if (!conta.Contains("revenue")) { return; }
                //Entity conta = Service.Retrieve("account", entityContext.Id, new ColumnSet("creditlimit", "revenue"));

                //throw new InvalidPluginExecutionException("Erro do try catch");//Mostrar o efeito
                Entity contaUpdate = new Entity(entityContext.LogicalName, entityContext.Id);
                if (((Money)conta["creditlimit"]).Value > ((Money)conta["revenue"]).Value * new decimal(0.5))
                {
                    //conta["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Reprovado);//Errado!
                    contaUpdate["curso_statusaprovacaolimite"] = new OptionSetValue((int)StatusLimite.Reprovado);//Correto
                    string mensagem = "Limite de Crédito não pode exceder o valor de R$" + Decimal.Round(((Money)conta["revenue"]).Value * new decimal(0.5), 2).ToString();
                    contaUpdate["curso_errovalidacao"] = mensagem;
                    Context.OutputParameters["retorno"] = mensagem;
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
