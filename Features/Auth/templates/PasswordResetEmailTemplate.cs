public static class PasswordResetEmailTemplate
{
    public static string Generate(string resetLink)
    {
        return $@"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Recuperação de Senha</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .header {{
            background-color: #007bff;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 8px 8px 0 0;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
        }}
        .content {{
            padding: 30px 20px;
        }}
        .content p {{
            color: #333;
            margin-bottom: 20px;
        }}
        .button {{
            display: inline-block;
            background-color: #007bff;
            color: white;
            padding: 12px 30px;
            text-decoration: none;
            border-radius: 5px;
            font-weight: bold;
            margin: 20px 0;
        }}
        .button:hover {{
            background-color: #0056b3;
        }}
        .footer {{
            text-align: center;
            padding: 20px;
            color: #666;
            font-size: 12px;
            border-top: 1px solid #e0e0e0;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 10px;
            margin: 20px 0;
            color: #856404;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Recuperação de Senha</h1>
        </div>
        <div class='content'>
            <p>Olá,</p>
            <p>Recebemos uma solicitação para redefinir a senha da sua conta. Se você não fez essa solicitação, pode ignorar este email.</p>
            <p>Para redefinir sua senha, clique no botão abaixo:</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Redefinir Senha</a>
            </p>
            <p>Ou copie e cole o seguinte link no seu navegador:</p>
            <p style='word-break: break-all; color: #007bff;'>{resetLink}</p>
            <div class='warning'>
                <strong>Atenção:</strong> Este link expirará em 15 minutos por motivos de segurança.
            </div>
        </div>
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
        </div>
    </div>
</body>
</html>";
    }
}
