using Curso.AprovacaoDeCredito.Biblioteca;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Curso.AprovacaoDeCredito
{
    public class AprovacaoCredito : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            #region "Parâmetros essenciais para o plugin"
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);//Contexto do Usuário
            IOrganizationService serviceGlobal = serviceFactory.CreateOrganizationService(null);//Contexto Global
            ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            Helper helper = new Helper(serviceGlobal, context, tracing);
            #endregion

            /*
             * Desenvolver um Plug-In na alteração para Aprovar/Reprovar a tarefa de Limite vai ser encerrada
             * Mensagem de update: Ideal para validar regras e executar ações na própria etidade ou entidades relacionadas
             * Pré-Imagem: Guarda as informações do registro antes da ação, nem todas ações podem ter preimage, alguns exemplos que podem são: update e delete
            */
            if (context.MessageName.ToLower().Trim() == StepMessage.update.ToString())
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    Entity entityContext = context.InputParameters["Target"] as Entity;
                    /*
                     * Campos da Pré Imagem
                     * regardingobjectid
                    */
                    if (context.PreEntityImages.Contains("PreImage") && context.PreEntityImages["PreImage"] is Entity)
                    {
                        Entity preImage = context.PreEntityImages["PreImage"] as Entity;
                        /*
                         * Campos em que a alteração deve rodar
                         * curso_statusaprovacaolimite
                        */
                        if (context.Stage == (int)StepEventStage.PostOperation && context.Mode == (int)StepExecutionMode.Synchronous)
                        {
                            helper.ConcluirTarefa(entityContext, preImage);
                        }
                    }
                }
            }

            /*
             * Criar Plugin na alteração de status para obrigar estar em concordância com o campo de Status da Aprovação
             * Mensagem de setstatedynamicentity: Ideal para validar regras e executar ações quando o STATUS é alterado
             * OBS: A entidade relacionada é trazida como referência
            */
            if (context.MessageName.ToLower() == "setstatedynamicentity")
            {
                if (!context.InputParameters.Contains("EntityMoniker")) { return; }
                if (!context.InputParameters.Contains("State")) { return; }

                EntityReference entidade = (EntityReference)context.InputParameters["EntityMoniker"];
                OptionSetValue state = (OptionSetValue)context.InputParameters["State"];

                if (context.Stage == (int)StepEventStage.PreOperation)
                {
                    helper.ValidarConclusaoTarefa(entidade, state);
                }
            }

            /*
             * Criar Plugin na criação para não deixar criar Tarefa de Limite se existir um limite aprovado com o valor maior
             * Mensagem de create: Ideal para validar se a criação do registro é válida ou disparar ações a partir da criação do registro
            */
            if (context.MessageName.ToLower() == "create")
            {
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    Entity entityContext = context.InputParameters["Target"] as Entity;
                    if (context.Stage == (int)StepEventStage.PreValidation)
                        helper.ValidarCriacao(entityContext);
                }
            }
        }
    }
}
