using PortalGamerX.Domain;
using PortalGamerX.Domain.Rules;

namespace PortalGamerX.Tests.Domain;

public class OrderStateMachineTests
{
    [Theory]
    [InlineData(OrderStatus.PendingPayment, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.PendingPayment, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.PendingPayment, OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.PendingPayment, false)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    public void Transicoes(OrderStatus from, OrderStatus to, bool expected) =>
        Assert.Equal(expected, OrderStateMachine.CanTransition(from, to));

    [Fact]
    public void Admin_nunca_pode_marcar_como_pago()
    {
        foreach (var s in Enum.GetValues<OrderStatus>())
            Assert.DoesNotContain(OrderStatus.Paid, OrderStateMachine.AdminTargets(s));
    }

    [Fact]
    public void Estados_finais()
    {
        Assert.True(OrderStateMachine.IsFinal(OrderStatus.Delivered));
        Assert.True(OrderStateMachine.IsFinal(OrderStatus.Cancelled));
        Assert.False(OrderStateMachine.IsFinal(OrderStatus.Paid));
    }

    [Fact]
    public void Regras_de_cancelamento_do_cliente()
    {
        Assert.True(OrderStateMachine.CustomerCanCancelDirectly(OrderStatus.PendingPayment));
        Assert.False(OrderStateMachine.CustomerCanCancelDirectly(OrderStatus.Paid));
        Assert.True(OrderStateMachine.CustomerCanRequestCancellation(OrderStatus.Paid));
        Assert.False(OrderStateMachine.CustomerCanRequestCancellation(OrderStatus.Shipped));
    }
}
