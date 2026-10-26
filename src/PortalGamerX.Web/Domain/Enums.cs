namespace PortalGamerX.Domain;

public enum OrderStatus { PendingPayment, Paid, Processing, Shipped, Delivered, Cancelled }

public enum PaymentStatus { Pending, InProcess, Approved, Rejected, Cancelled, Refunded, ChargedBack }

public enum DiscountType { Percentage, FixedAmount }

public enum StockMovementType { Entry, Exit, Adjustment, OrderReservation, OrderRelease }

public enum EmailStatus { Pending, Sent, Failed }

public enum FiscalStatus { NotRequested, Processing, Authorized, Rejected, Cancelled, Error }

/// <summary>Plataforma do jogo. Valores numéricos fixos (gravados no banco): não reordene.</summary>
public enum GamePlatform
{
    Pc = 0,
    PlayStation5 = 1,
    PlayStation4 = 2,
    XboxSeries = 3,
    XboxOne = 4,
    NintendoSwitch = 5,
    NintendoSwitch2 = 6,
    Outro = 99
}

/// <summary>Loja/plataforma onde a key é ativada (resgatada). Valores fixos (gravados no banco): não reordene.</summary>
public enum ActivationStore
{
    Steam = 0,
    EpicGames = 1,
    Gog = 2,
    EaApp = 3,
    UbisoftConnect = 4,
    BattleNet = 5,
    PlayStationStore = 6,
    XboxMicrosoftStore = 7,
    NintendoEshop = 8,
    Outra = 99
}

/// <summary>Região em que a key pode ser ativada (keys têm trava regional).</summary>
public enum KeyRegion
{
    Global = 0,
    Brasil = 1,
    AmericaLatina = 2,
    AmericaDoNorte = 3,
    Europa = 4,
    Outra = 99
}

/// <summary>Situação de uma key no inventário. Valores fixos (gravados no banco).</summary>
public enum GameKeyStatus
{
    /// <summary>Em estoque, livre para venda.</summary>
    Available = 0,
    /// <summary>Reservada para um pedido ainda não pago.</summary>
    Reserved = 1,
    /// <summary>Entregue ao cliente (pedido pago).</summary>
    Sold = 2,
    /// <summary>Invalidada pelo administrador (defeituosa, já usada, reembolsada...). Nunca volta ao estoque.</summary>
    Void = 3
}

/// <summary>Classificação indicativa (ClassInd). O valor é a idade mínima; 0 = livre.</summary>
public enum AgeRating { Livre = 0, Anos10 = 10, Anos12 = 12, Anos14 = 14, Anos16 = 16, Anos18 = 18 }

public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
}

public static class EnumLabels
{
    public static string Label(this OrderStatus s) => s switch
    {
        OrderStatus.PendingPayment => "Aguardando pagamento",
        OrderStatus.Paid => "Pago",
        OrderStatus.Processing => "Em preparação",
        OrderStatus.Shipped => "Enviado",
        OrderStatus.Delivered => "Entregue (keys liberadas)",
        OrderStatus.Cancelled => "Cancelado",
        _ => s.ToString()
    };

    public static string Label(this PaymentStatus s) => s switch
    {
        PaymentStatus.Pending => "Pendente",
        PaymentStatus.InProcess => "Em análise",
        PaymentStatus.Approved => "Aprovado",
        PaymentStatus.Rejected => "Recusado",
        PaymentStatus.Cancelled => "Cancelado",
        PaymentStatus.Refunded => "Reembolsado",
        PaymentStatus.ChargedBack => "Estornado (chargeback)",
        _ => s.ToString()
    };

    public static string Label(this StockMovementType t) => t switch
    {
        StockMovementType.Entry => "Entrada",
        StockMovementType.Exit => "Saída",
        StockMovementType.Adjustment => "Ajuste",
        StockMovementType.OrderReservation => "Reserva (pedido)",
        StockMovementType.OrderRelease => "Devolução ao estoque",
        _ => t.ToString()
    };

    public static string Label(this FiscalStatus s) => s switch
    {
        FiscalStatus.NotRequested => "Não solicitada",
        FiscalStatus.Processing => "Em processamento",
        FiscalStatus.Authorized => "Autorizada",
        FiscalStatus.Rejected => "Rejeitada",
        FiscalStatus.Cancelled => "Cancelada",
        FiscalStatus.Error => "Erro",
        _ => s.ToString()
    };

    public static string Label(this GamePlatform p) => p switch
    {
        GamePlatform.Pc => "PC",
        GamePlatform.PlayStation5 => "PlayStation 5",
        GamePlatform.PlayStation4 => "PlayStation 4",
        GamePlatform.XboxSeries => "Xbox Series X|S",
        GamePlatform.XboxOne => "Xbox One",
        GamePlatform.NintendoSwitch => "Nintendo Switch",
        GamePlatform.NintendoSwitch2 => "Nintendo Switch 2",
        GamePlatform.Outro => "Outra plataforma",
        _ => p.ToString()
    };

    public static string Label(this ActivationStore t) => t switch
    {
        ActivationStore.Steam => "Steam",
        ActivationStore.EpicGames => "Epic Games Store",
        ActivationStore.Gog => "GOG",
        ActivationStore.EaApp => "EA app",
        ActivationStore.UbisoftConnect => "Ubisoft Connect",
        ActivationStore.BattleNet => "Battle.net",
        ActivationStore.PlayStationStore => "PlayStation Store",
        ActivationStore.XboxMicrosoftStore => "Xbox / Microsoft Store",
        ActivationStore.NintendoEshop => "Nintendo eShop",
        ActivationStore.Outra => "Outra loja",
        _ => t.ToString()
    };

    public static string Label(this KeyRegion r) => r switch
    {
        KeyRegion.Global => "Global",
        KeyRegion.Brasil => "Brasil",
        KeyRegion.AmericaLatina => "América Latina",
        KeyRegion.AmericaDoNorte => "América do Norte",
        KeyRegion.Europa => "Europa",
        KeyRegion.Outra => "Outra região",
        _ => r.ToString()
    };

    public static string Label(this GameKeyStatus s) => s switch
    {
        GameKeyStatus.Available => "Disponível",
        GameKeyStatus.Reserved => "Reservada",
        GameKeyStatus.Sold => "Vendida",
        GameKeyStatus.Void => "Invalidada",
        _ => s.ToString()
    };

    public static string Label(this AgeRating a) => a == AgeRating.Livre ? "Livre" : $"{(int)a} anos";

    public static string Badge(this OrderStatus s) => s switch
    {
        OrderStatus.PendingPayment => "warning",
        OrderStatus.Paid => "success",
        OrderStatus.Processing => "info",
        OrderStatus.Shipped => "primary",
        OrderStatus.Delivered => "success",
        OrderStatus.Cancelled => "secondary",
        _ => "light"
    };

    public static string Badge(this PaymentStatus s) => s switch
    {
        PaymentStatus.Approved => "success",
        PaymentStatus.Pending or PaymentStatus.InProcess => "warning",
        PaymentStatus.Rejected or PaymentStatus.ChargedBack => "danger",
        _ => "secondary"
    };
}
