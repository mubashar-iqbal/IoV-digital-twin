
const { expect } = require("chai");


describe("RSU", function() {

    // it("Should deploy RSU, Vehicle and Analyzer Vehicle and return their addresses", async function() {

    //     // get the road side unit factory
    //     const RoadSideUnit = await hre.ethers.getContractFactory("RoadSideUnit");

    //     //deploy the rsu contract which also deploys vehicle and analyzer vehicle contracts
    //     const rsu = await RoadSideUnit.deploy();

    //     // wait for the deployment completion
    //     await rsu.deployed();

    //     //Get all the addresses

    //     const [vehcileAddr, analyzerVehicleAddr] = await rsu.getChildContractAddresses();

    //     // Check the addresses are non-zero
    //     // expect(rsu.address).to.not.equal(ethers.contains.AddressZero);
    //     // expect(vehcileAddr).to.not.equal(ethers.contains.AddressZero);
    //     // expect(analyzerVehicleAddr).to.not.equal(ethers.contains.AddressZero);

    //     // Check the addresses are distinct
    //     expect(rsu.address).to.not.equal(vehcileAddr);
    //     expect(rsu.address).to.not.equal(analyzerVehicleAddr);

    //     expect(vehcileAddr).to.not.equal(rsu.address);
    //     expect(vehcileAddr).to.not.equal(analyzerVehicleAddr);

    //     expect(analyzerVehicleAddr).to.not.equal(rsu.address);
    //     expect(analyzerVehicleAddr).to.not.equal(vehcileAddr);


    // });


    it("It tries to set temperature to vehicle contract deployed", async () => {


        // get the road side unit factory
        const Vehicle = await hre.ethers.getContractFactory("Vehicle");

        //deploy the rsu contract which also deploys vehicle and analyzer vehicle contracts
        const vehicle = await Vehicle.deploy();

        // wait for the deployment completion
        await vehicle.deployed();
        
        console.log(`Vehicle deployed: ${vehicle.address}`)

        const setTemp = await vehicle.setTemp(70);
        console.log(`Vehcle Temp Set: ${setTemp.address}`);

        const temperature = await vehicle.getTemp();
        expect(temperature).to.be.equal(70);
    });


});

// RSU deployed to:  0x5FbDB2315678afecb367f032d93F642f64180aa3
// Vehcle Contract deployed to:  0xa16E02E87b7454126E5E10d957A927A7F5B5d2be
// Ananlyzer Contract deployed to:  0xB7A5bd0345EF1Cc5E66bf61BdeC17D2461fBd968