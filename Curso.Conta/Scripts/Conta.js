if (typeof (Curso) === "undefined") { Curso = {}; }

Curso = {
    campoReceitaAnual: "revenue",
    campoLimiteDeCredito: "creditlimit",
    sucessoRetornoAcao: "",
    falhaRetornoAcao: "",

    ValidarLimite: function () {
        debugger;
        var idConta = Xrm.Page.data.entity.getId();
        //Boa prática redefinir as variáveis globais
        Curso.sucessoRetornoAcao = "";
        Curso.falhaRetornoAcao = "";

        if (!idConta) {
            return;
        }

        Xrm.Page.data.entity.save();

        idConta = idConta.replace("{", "").replace("}", "");
        Curso.ExecutarAcao(Xrm.Page.context.getClientUrl() + "/api/data/v9.1/accounts(" + idConta + ")/Microsoft.Dynamics.CRM.curso_validarlimite", "POST");
        if (Curso.falhaRetornoAcao !== "") {
            Demo.Helper.ExibirAlertaSemConfirmacao(Curso.falhaRetornoAcao, "Falha");
            return;
        }

        if (Curso.sucessoRetornoAcao.retorno !== null) {
            Demo.Helper.ExibirAlertaSemConfirmacao(Curso.sucessoRetornoAcao.retorno, "Falha");
            Xrm.Page.data.refresh();
            return;
        }

        Demo.Helper.ExibirAlertaSemConfirmacao("Conta Validada com Sucesso!", "Sucesso");
        Xrm.Page.data.refresh();
    },

    ExecutarAcao: function (url, metodo, data = "") {
        if (data === "")
            data = "";
        else
            data = window.JSON.stringify(data);

        jQuery.ajax({
            type: metodo,
            contentType: "application/json; charset=utf-8",
            datatype: "json",
            data: data,
            url: url,
            async: false,
            beforeSend: function (XMLHttpRequest) {
                XMLHttpRequest.setRequestHeader("Accept", "application/json");
                XMLHttpRequest.setRequestHeader("Prefer", "odata.include-annotations=*");
            },
            success: function (data) {
                if (data)
                    Curso.sucessoRetornoAcao = data;
            },
            error: function (XmlHttpRequest) {
                if (XmlHttpRequest.responseJSON.error.message)
                    Curso.falhaRetornoAcao = XmlHttpRequest.responseJSON.error.message;
            }
        });
    }
};
