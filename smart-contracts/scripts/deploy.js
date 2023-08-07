
async function main() {
  
  // get the road side unit factory
  const RoadSideUnit = await hre.ethers.getContractFactory("RoadSideUnit");

  //deploy the rsu contract which also deploys vehicle and analyzer vehicle contracts
  const rsu = await RoadSideUnit.deploy();

  // wait for the deployment completion
  await rsu.deployed();

  //Get all the addresses

  const [vehcileAddr, vehicleAnalyzerAddr] = await rsu.getChildContractAddresses();


  console.log(`RSU deployed to:  ${rsu.address}`);
  console.log(`Vehcle Contract deployed to:  ${vehcileAddr}`);
  console.log(`Analyzer Contract deployed to:  ${vehicleAnalyzerAddr}`);  
}

// We recommend this pattern to be able to use async/await everywhere
// and properly handle errors.
main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
