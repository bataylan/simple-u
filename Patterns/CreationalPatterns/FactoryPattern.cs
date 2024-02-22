
namespace SimpleU.Pattern.Factory
{
    //Creational Pattern
    //every product must have own creator class
    //good for few options
    //bad for many options
    //if/else or switch/case required
    public class ExampleUsage
    {
        private Factory creator;

        public void Run()
        {
            string input = "A";

            if (input.Equals("A")) //case for A
            {
                creator = new ConcreteFactoryA();
            }
            else if (input.Equals("B")) //case for B
            {
                creator = new ConcreteFactoryB();
            }
            else
            {
                throw new System.Exception("Error!");
            }
        }
    }

    /// <summary>
    /// Base class for creators
    /// </summary>
    public abstract class Factory
    {
        public void Stuff()
        {
            var product = CreateProduct();
            product.DoStuff();
        }

        public abstract IProduct CreateProduct();
    }

    public class ConcreteFactoryA : Factory
    {
        public override IProduct CreateProduct()
        {
            return new ConcreteProducA();
        }
    }

    public class ConcreteFactoryB : Factory
    {
        public override IProduct CreateProduct()
        {
            return new ConcreteProducB();
        }
    }

    public interface IProduct
    {
        void DoStuff();
    }

    public class ConcreteProducA : IProduct
    {
        public void DoStuff()
        {
            throw new System.NotImplementedException();
        }
    }

    public class ConcreteProducB : IProduct
    {
        public void DoStuff()
        {
            throw new System.NotImplementedException();
        }
    }
}