
namespace SimpleU.Pattern.Factory
{
    //Creational Pattern
    //every product must have own creator class
    //good for few options
    //bad for many options
    //if/else or switch/case required
    public class ExampleUsage
    {
        private Creator creator;

        public void Run()
        {
            string input = "A";

            if (input.Equals("A")) //case for A
            {
                creator = new ConcreteCreatorA();
            }
            else if (input.Equals("B")) //case for B
            {
                creator = new ConcreteCreatorB();
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
    public abstract class Creator
    {
        public void Stuff()
        {
            var product = CreateProduct();
            product.DoStuff();
        }

        public abstract IProduct CreateProduct();
    }

    public class ConcreteCreatorA : Creator
    {
        public override IProduct CreateProduct()
        {
            return new ConcreteProducA();
        }
    }

    public class ConcreteCreatorB : Creator
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