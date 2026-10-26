using PortalGamerX.Domain;
using PortalGamerX.Domain.Rules;

namespace PortalGamerX.Tests.Domain;

public class OrderStateMachineTests
{
    [Theory]
    [InlineData(OrderStatus.PendingPayment, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.PendingPayment, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.PendingPayment, OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Paid, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.PendingPayment, false)]
    // reembolso/chargeback de pedido já entregue (keys são invalidadas)
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Delivered, false)]
    // estados legados (venda de mídia física) continuam coerentes
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered, true)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled, false)]
    public void Transicoes(OrderStatus from, OrderStatus to, bool expected) =>
        Assert.Equal(expected, OrderStateMachine.CanTransition(from, to));

    [Fact]
    public void Admin_nunca_marca_como_pago_nem_entregue()
    {
        foreach (var s in Enum.GetValues<OrderStatus>())
        {
            Assert.DoesNotContain(OrderStatus.Paid, OrderStateMachine.AdminTargets(s));
            Assert.DoesNotContain(OrderStatus.Delivered, OrderStateMachine.AdminTargets(s));
        }
    }

    [Fact]
    public void Admin_so_cancela_pedidos_nao_entregues()
    {
        Assert.Equal(new[] { OrderStatus.Cancelled }, OrderStateMachine.AdminTargets(OrderStatus.PendingPayment));
        Assert.Equal(new[] { OrderStatus.Cancelled }, OrderStateMachine.AdminTargets(OrderStatus.Paid));
        Assert.Empty(OrderStateMachine.AdminTargets(OrderStatus.Delivered));
        Assert.Empty(OrderStateMachine.AdminTargets(OrderStatus.Cancelled));
    }

    [Fact]
    public void Estados_finais()
    {
        Assert.True(OrderStateMachine.IsFinal(OrderStatus.Delivered));
        Assert.True(OrderStateMachine.IsFinal(OrderStatus.Cancelled));
        Assert.False(OrderStateMachine.IsFinal(OrderStatus.Paid));
    }

    [Fact]
    public void Cliente_so_cancela_direto_antes_do_pagamento()
    {
        Assert.True(OrderStateMachine.CustomerCanCancelDirectly(OrderStatus.PendingPayment));
        Assert.False(OrderStateMachine.CustomerCanCancelDirectly(OrderStatus.Paid));
        Assert.False(OrderStateMachine.CustomerCanCancelDirectly(OrderStatus.Delivered));
        Assert.False(OrderStateMachine.CustomerCanRequestCancellation(OrderStatus.Paid));
    }
}
