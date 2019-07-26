namespace CatLibrary
{
    //interface
    /// <summary>
    /// This is <b>basic</b> interface of all animal.
    /// </summary>
    public interface IAnimal
    {
        //property
        /// <summary>
        /// Name of Animal.
        /// </summary>
        string Name { get; }
        //index
        /// <summary>
        /// Return specific number animal's name.
        /// </summary>
        /// <param name="index">Animal number.</param>
        /// <returns>Animal name.</returns>
        string this[int index] { get; }
        //method
        /// <summary>
        /// Animal's eat method.
        /// </summary>
        void Eat();
        //template method
        /// <summary>
        /// Overload method of eat. This define the animal eat by which tool.
        /// </summary>
        /// <typeparam name="Tool">It's a class type.</typeparam>
        /// <param name="tool">Tool name.</param>
        void Eat<Tool>(Tool tool)
            where Tool : class;

        /// <summary>
        /// Feed the animal with some food
        /// </summary>
        /// <param name="food">Food to eat</param>
        void Eat(string food);
    }
}
