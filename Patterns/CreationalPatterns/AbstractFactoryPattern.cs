

namespace SimpleU.Pattern.AbstractFactory
{
    //Creational Pattern
    //factories for variations
    //every product has interface
    //every variation has class
    //good for static products, but dynamic variations
    public class ExampleUsage
    {
        private IAbstractFactory abstractFactory;
        private IAbstractProductA productA;
        private IAbstractProductB productB;

        public ExampleUsage(IAbstractFactory abstractFactory)
        {
            this.abstractFactory = abstractFactory;
        }

        public void CreateProductA()
        {
            productA = abstractFactory.CreateProductA();
            productB = abstractFactory.CreateProductB();
        }

        public void DoStuff()
        {
            productA.DoStuff();
            productB.DoStuff();
        }
    }

    public interface IAbstractFactory
    {
        IAbstractProductA CreateProductA();
        IAbstractProductB CreateProductB();
    }

    //factory for variations
    public class ConcreteFactory1 : IAbstractFactory
    {
        public IAbstractProductA CreateProductA() { return new ConcreteProductA1(); }

        public IAbstractProductB CreateProductB() { return new ConcreteProductB1(); }
    }

    public class ConcreteFactory2 : IAbstractFactory
    {
        public IAbstractProductA CreateProductA() { return new ConcreteProductA2(); }

        public IAbstractProductB CreateProductB() { return new ConcreteProductB2(); }
    }

    //interface for products
    public interface IAbstractProductA
    {
        void DoStuff();
    }

    //class for product variations
    public class ConcreteProductA1 : IAbstractProductA
    {
        public void DoStuff() { }
    }

    public class ConcreteProductA2 : IAbstractProductA
    {
        public void DoStuff() { }
    }

    public interface IAbstractProductB
    {
        void DoStuff();
    }

    public class ConcreteProductB1 : IAbstractProductB
    {
        public void DoStuff() { }
    }

    public class ConcreteProductB2 : IAbstractProductB
    {
        public void DoStuff() { }
    }
}