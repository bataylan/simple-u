
namespace SimpleU.Pattern.Builder
{
    //good for few variations
    //steps can be implemented different
    //director abstraction, builders implementation
    public class ExampleUsage
    {
        public void Run()
        {
            var b = new ConcreteBuilder1();
            var d = new Director(b);
            d.BuildSimple();

            Product1 product1 = b.GetResult();
        }
    }

    //optional class
    public class Director
    {
        private IBuilder _builder;

        public Director(IBuilder builder)
        {
            _builder = builder;
        }

        public void ChangeBuilder(IBuilder builder)
        {
            _builder = builder;
        }

        public void BuildSimple()
        {
            _builder.BuildStepA();
        }

        public void BuildFull()
        {
            _builder.BuildStepA();
            _builder.BuildStepB();
            _builder.BuildStepZ();
        }
    }

    public interface IBuilder
    {
        void Reset();
        void BuildStepA();
        void BuildStepB();
        void BuildStepZ();
    }

    public class ConcreteBuilder1 : IBuilder
    {
        private Product1 _product1;

        public ConcreteBuilder1()
        {
            Reset();
        }

        public void BuildStepA() { }

        public void BuildStepB() { }

        public void BuildStepZ() { }

        public void Reset()
        {
            _product1 = new Product1();
        }

        public Product1 GetResult()
        {
            var result = _product1;
            Reset();
            return result;
        }
    }

    public class ConcreteBuilder2 : IBuilder
    {
        private Product2 _product2;

        public ConcreteBuilder2()
        {
            Reset();
        }

        public void BuildStepA() { }

        public void BuildStepB() { }

        public void BuildStepZ() { }

        public void Reset()
        {
            _product2 = new Product2();
        }

        public Product2 GetResult()
        {
            var result = _product2;
            Reset();
            return result;
        }
    }

    public class Product1
    {

    }

    public class Product2
    {

    }
}
