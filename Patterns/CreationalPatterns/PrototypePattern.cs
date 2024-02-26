using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleU.Pattern.Prototype
{
    //delegate clone process to original class (useful for private fields)
    //good for access modifiers and basic objects
    public class ExampleUsage
    {
        public void Run()
        {
            var prototype = new ConcretePrototype();
            prototype.Init(0);

            var cloneOne = prototype.DeepClone();
            var cloneTwo = prototype.ShallowClone();
        }
    }

    public interface IPrototype
    {
        IPrototype ShallowClone();
        IPrototype DeepClone();
    }

    public class ConcretePrototype : IPrototype
    {
        private int _id;
        private string _color;

        public void Init(int id)
        {
            _id = id;
        }

        public IPrototype DeepClone()
        {
            var clone = new ConcretePrototype();

            clone._id = _id;
            clone._color = _color;

            return clone;
        }

        public IPrototype ShallowClone()
        {
            var clone = new ConcretePrototype();

            clone._color = _color;

            return new ConcretePrototype();
        }
    }
}
