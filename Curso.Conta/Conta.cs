﻿using Curso.Conta.Biblioteca;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Curso.Conta
{
    public class Conta : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            #region "Parâmetros essenciais para o plugin"
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);//Contexto do Usuário
            IOrganizationService serviceGlobal = serviceFactory.CreateOrganizationService(null);//Contexto Global
            ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));//Guarda um histórico de etapas do código
            Helper helper = new Helper(serviceGlobal, service, context, tracing);
            #endregion

            /*
             * Desenvolver Plug-In para criar aprovação de limite de crédito
             * Mensagem de update: Ideal para validar regras e executar ações na própria etidade ou entidades relacionadas
            */
            if (context.MessageName.ToLower() == StepMessage.update.ToString())
            {
                //O Target contém a entidade alterada, no caso do update, mas somente os campos que sofreram alteração
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    Entity entityContext = context.InputParameters["Target"] as Entity;

                    if (context.Stage == (int)StepEventStage.PreOperation)
                    {
                        helper.CriarLimiteDeCredito(entityContext);
                    }
                }
            }

            /*
             * Desenvolver Plug-In para validar de limites de créditos
             * Mensagem customizada: Ideal para quando é necessário executar alguma ação que seja necessário ter um retorno de sucesso ou erro
            */
            if (context.MessageName.ToLower() == "curso_validarlimite")
            {
                //Quando é ação customizada, eu tenho somente a referência do registro
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is EntityReference)
                {
                    EntityReference entityContext = context.InputParameters["Target"] as EntityReference;

                    if (context.Stage == (int)StepEventStage.PostOperation && context.Mode == (int)StepExecutionMode.Synchronous)
                        helper.ValidarLimite(entityContext);
                }
            }
        }
    }
}
